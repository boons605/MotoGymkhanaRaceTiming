/*
 * MGBTManager.c
 *
 *  Created on: 21 Nov 2020
 *      Author: cdromke
 */

#include <stdint.h>
#include "MGBTManager.h"
#include "MGBTCommProto.h"
#include "MGBTDevice.h"
#include "MGBTTimeMgmt.h"

#include "esp_ibeacon_api.h"
#include "esp_log.h"
#include "driver/gpio.h"

#define GPIO_OUTPUT_IO_0    18
#define GPIO_OUTPUT_IO_1    17
#define GPIO_OUTPUT_IO_2    16
#define GPIO_OUTPUT_PIN_SEL  ((1ULL<<GPIO_OUTPUT_IO_0) | (1ULL<<GPIO_OUTPUT_IO_1) | (1ULL<<GPIO_OUTPUT_IO_2))

static const char* AppName = "MGBTManager";

static MGBTDeviceData deviceList[MAXDEVICES] = {0};
static MGBTCommandData pendingResponse = {0};
static uint8_t lastResponseSent = 0U;
static uint32_t lastTimeCleanup = 0U;
static uint32_t lastTimeClosestDevice = 0U;
static uint32_t scanStartTime = 0U;
static uint32_t lastProgressPacketTime = 0U;
static uint8_t startLightState = 0U;

static uint8_t totalPackets;
static uint8_t currentPacket;
static uint8_t currentDeviceIndex;


static ManagerState managerState = MgBtMState_Idle;

void InitManager(void)
{
    InitCommProto();
    lastTimeCleanup = GetTimestampMs();
    lastTimeClosestDevice = lastTimeCleanup;

    gpio_config_t io_conf;
	//disable interrupt
	io_conf.intr_type = GPIO_PIN_INTR_DISABLE;
	//set as output mode
	io_conf.mode = GPIO_MODE_OUTPUT;
	//bit mask of the pins that you want to set,e.g.GPIO18/19
	io_conf.pin_bit_mask = GPIO_OUTPUT_PIN_SEL;
	//disable pull-down mode
	io_conf.pull_down_en = 0;
	//disable pull-up mode
	io_conf.pull_up_en = 0;
	//configure GPIO with the given settings
	gpio_config(&io_conf);
}

static void FindDeviceOrFirstFreeIndex(uint8_t* address, uint16_t* firstFreeIndex, uint16_t* indexFound)
{

    uint16_t index = 0U;


    (*firstFreeIndex) = MAXDEVICES;
    (*indexFound) = MAXDEVICES;

    while(index < MAXDEVICES)
    {
        if(IsDeviceEntryEmpty(&deviceList[index]) == 0U)
        {
            if(BTDeviceAddressEquals(&deviceList[index], address) == 1U)
            {
                (*indexFound) = index;
            }
        }
        else
        {
            if((*firstFreeIndex) == MAXDEVICES)
            {
                (*firstFreeIndex) = index;
            }
        }
        index++;

        if(((*indexFound) != MAXDEVICES) && ((*firstFreeIndex) != MAXDEVICES))
        {
            index = MAXDEVICES;
        }
    }
}

static uint8_t CountDevices(uint8_t active, uint8_t allowed)
{
    uint8_t retVal = 0U;

    uint8_t index = 0U;
    for(index = 0U; index < MAXDEVICES; index++)
    {
        MGBTDeviceData* device = &deviceList[index];

        if(IsDeviceEntryEmpty(device))
        {
            if((active > 1U) || (IsDeviceActive(device) == active))
            {
				if((device->allowed == allowed) || (allowed > 1U))
				{
					retVal++;
				}
            }
        }

        if(deviceList[index].allowed == 1U)
        {
            retVal++;
        }
    }

    return retVal;
}

static uint8_t CountAllowedDevices(void)
{
    return CountDevices(2U, 1U);
}



static void AddDeviceToListAtIndex(uint16_t index, uint8_t* address, uint8_t allowed, int8_t mpCorrection)
{
    if(index < MAXDEVICES)
    {
        MGBTDeviceData* device = &deviceList[index];
        memcpy(device->device.address, address, ESP_BD_ADDR_LEN);
        device->allowed = allowed;
        device->device.measuredPowerCorrection = mpCorrection;
    }
}

static void AddDeviceToList(MGBTAllowedDeviceEntry* entry, uint8_t allowed)
{
    uint16_t firstFreeIndex = MAXDEVICES;
    uint16_t indexFound = MAXDEVICES;

    FindDeviceOrFirstFreeIndex(entry->address, &firstFreeIndex, &indexFound);

    if(indexFound == MAXDEVICES)
    {
        if(firstFreeIndex != MAXDEVICES)
        {
            AddDeviceToListAtIndex(firstFreeIndex, entry->address, allowed, entry->measuredPowerCorrection);
            pendingResponse.status = 0U;
            esp_log_buffer_hex("Added device:", entry->address, ESP_BD_ADDR_LEN);
        }
        else
        {
            pendingResponse.status = 0xFFFEU;
        }
    }
    else
    {
        MGBTDeviceData* device = &deviceList[indexFound];
        if(device->allowed == 0U)
        {
            device->allowed = 0U;
            pendingResponse.status = 0U;
        }
        else
        {
            pendingResponse.status = 0xFFFFU;
        }
    }
}

static void RemoveDeviceFromList(uint8_t* address)
{
    uint16_t firstFreeIndex = MAXDEVICES;
    uint16_t indexFound = MAXDEVICES;

    FindDeviceOrFirstFreeIndex(address, &firstFreeIndex, &indexFound);

    if(indexFound != MAXDEVICES)
    {
        ClearDeviceEntry(&deviceList[indexFound]);
        pendingResponse.status = 0U;
    }
    else
    {
        pendingResponse.status = 0xFFFFU;
    }
}

static uint8_t SpaceLeftInPacket(void)
{
    return COMMANDDATAMAXSIZE - pendingResponse.dataLength;
}

static void PrepareTransportPacket(uint8_t totalDevices)
{
    uint8_t devicesPerPacket = (uint8_t)((GetCommandMaxDataLength() - 2U) / sizeof(MGBTDevice));

    if(totalDevices < devicesPerPacket)
    {
        totalPackets = 1U;
    }
    else
    {
        totalPackets = (totalDevices / devicesPerPacket);
        if((totalDevices % devicesPerPacket) > 0)
        {
            totalPackets++;
        }
    }

    currentDeviceIndex = 0;
    currentPacket = 0;
}

static void StartListingAllowedDevices(void)
{

    PrepareTransportPacket(CountAllowedDevices());
    managerState = MgBtMState_ListingAllowedDevices;
}

static void SendNextPacket(void)
{
    ClearPacketData(&pendingResponse);
    pendingResponse.status = 1U;
    pendingResponse.data[0] = currentPacket;
    currentPacket++;
    pendingResponse.data[1] = totalPackets;
    pendingResponse.dataLength = 2U;
}

static void SendListAllowedDevicePacket(void)
{
    SendNextPacket();
    ESP_LOGI(AppName, "Sending transport packet: AllowedDev");
    while((SpaceLeftInPacket() > sizeof(MGBTDevice)) &&
          (currentDeviceIndex < MAXDEVICES))
    {
        MGBTDeviceData* device = &deviceList[currentDeviceIndex];
        if(device->allowed != 0U)
        {
            memcpy(&pendingResponse.data[pendingResponse.dataLength], &device->device, sizeof(MGBTDevice));
            pendingResponse.dataLength += sizeof(MGBTDevice);
        }
        currentDeviceIndex++;
    }


    ESP_LOGI(AppName, "Sending transport packet: AllowedDev, device index: %d", currentDeviceIndex);

    if(currentDeviceIndex >= MAXDEVICES)
    {
        lastResponseSent = 1U;
    }
}

static uint32_t GetProgressInterval(void)
{
    return DEVICELISTPROGRESSINTERVAL;
}

static uint32_t GetScanDuration(void)
{
    return DEVICELISTSCANTIME;
}

static uint8_t GetFilterAllowedDevices(void)
{
    uint8_t retVal = 1U;

    if((managerState == MgBtMState_ListingAllDetectedDevices) &&
       (GetTimestampMs() < (scanStartTime + GetScanDuration())))
    {
        retVal = 0U;
    }

    return retVal;
}

static void StartListingAllDevices(void)
{
    managerState = MgBtMState_ListingAllDetectedDevices;
    scanStartTime = GetTimestampMs();
    lastProgressPacketTime = scanStartTime;
    currentPacket = 0U;
    totalPackets = 0U;
}

static void SendListAllDevicesProgress(void)
{
    if(GetTimestampMs() > (lastProgressPacketTime + GetProgressInterval()))
    {
        ESP_LOGI(AppName, "Sending progress packet");
        lastProgressPacketTime = GetTimestampMs();
        uint32_t progress = ((GetTimestampMs() - scanStartTime) * 100U) / GetScanDuration();
        pendingResponse.dataLength = 2U;
        pendingResponse.status = 8U;
        pendingResponse.data[0] = (uint8_t)progress;
        pendingResponse.data[1] = CountDevices(2U, 2U);

    }
}

static void SendListAllDevicesPacket(void)
{
    if(GetTimestampMs() < (scanStartTime + GetScanDuration()))
    {
        SendListAllDevicesProgress();
    }
    else
    {
        ESP_LOGI(AppName, "Sending transport packet: AllDev");
        if(totalPackets == 0U)
        {
            PrepareTransportPacket(CountDevices(2U, 2U));
        }
        SendNextPacket();
        while((SpaceLeftInPacket() > sizeof(MGBTDevice)) &&
              (currentDeviceIndex < MAXDEVICES))
        {
            MGBTDeviceData* device = &deviceList[currentDeviceIndex];
            if(IsDeviceEntryEmpty(device) == 0U)
            {
                memcpy(&pendingResponse.data[pendingResponse.dataLength], &device->device, sizeof(MGBTDevice));
                pendingResponse.dataLength += sizeof(MGBTDevice);
            }
            currentDeviceIndex++;
        }

        ESP_LOGI(AppName, "Sending transport packet: AllDev, device index: %d", currentDeviceIndex);
        if(currentDeviceIndex >= MAXDEVICES)
        {
            lastResponseSent = 1U;
        }

    }
}

static MGBTDeviceData* GetClosestDeviceFromList(void)
{
    MGBTDeviceData* retVal = (MGBTDeviceData*)0;
    uint16_t index = 0U;
    for(index = 0U; index < MAXDEVICES; index++)
    {
        MGBTDeviceData* entry = &deviceList[index];
        if(entry->allowed == 1U)
        {
            if(IsDeviceActive(entry) == 1U)
            {
                if(retVal == (MGBTDeviceData*)0)
                {
                    retVal = entry;
                }
                else
                {
                    if(entry->lastDistanceExponent < retVal->lastDistanceExponent)
                    {
                        retVal = entry;
                    }
                }
            }
        }
    }
    return retVal;
}

static void PrepareClosestDeviceData(void)
{
    MGBTDeviceData* device = GetClosestDeviceFromList();
    if(device != (MGBTDeviceData*)0)
    {
        memcpy(pendingResponse.data, &device->device, sizeof(MGBTDevice));
        pendingResponse.dataLength = sizeof(MGBTDevice);
        pendingResponse.status = 0U;
    }
    else
    {
        pendingResponse.status = 0xFFFFU;
    }
    lastResponseSent = 1U;
}

static void ProcessSetStartLight(uint8_t data)
{
	startLightState = data;
}

static void ProcessCommand(MGBTCommandData* command)
{
    lastResponseSent = 0U;
    switch(command->cmdType)
    {
        case AddAllowedDevice:
        {
            ESP_LOGI(AppName, "Add to allowed devices");
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            if (command->dataLength >= sizeof(MGBTAllowedDeviceEntry))
            {
            	AddDeviceToList((MGBTAllowedDeviceEntry*)command->data, 1U);
            }
            else
            {
            	pendingResponse.status = 0xFEFEU;
            }
            lastResponseSent = 1U;
            break;
        }
        case RemoveAllowedDevice:
        {
            ESP_LOGI(AppName, "Remove from allowed devices");
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            RemoveDeviceFromList(command->data);
            lastResponseSent = 1U;
            break;
        }
        case ClearAllowedDevices:
        {
        	ESP_LOGI(AppName, "Clear allowed devices");
			memcpy(&pendingResponse, command, GetCommandDataSize(command));
			memset(deviceList, 0U, sizeof(deviceList));
			lastResponseSent = 1U;
        	break;
        }
        case ListAllowedDevices:
        {
            ESP_LOGI(AppName, "List allowed devices");
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            StartListingAllowedDevices();
            break;
        }
        case ListDetectedDevices:
        {
            ESP_LOGI(AppName, "List detected devices");
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            StartListingAllDevices();
            //lastResponseSent = 1U;
            break;
        }
        case GetClosestDevice:
        {
            ESP_LOGI(AppName, "Get closest device");
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            PrepareClosestDeviceData();
            break;
        }
        case SetStartLightState:
		{
			ESP_LOGI(AppName, "Set start light state");
			memcpy(&pendingResponse, command, GetCommandDataSize(command));
			ProcessSetStartLight(command->data[0]);
			lastResponseSent = 1U;
			break;
		}
        default:
        {
            break;
        }
    }
}

static void CleanUpDeviceList(void)
{
    uint8_t index;
    for(index = 0U; index < MAXDEVICES; index++)
    {
        MGBTDeviceData* device = &deviceList[index];
        if((IsDeviceEntryEmpty(device) == 0U) &&
           (device->millisLastSeen > 0U))
        {
            if(IsDeviceActive(device) == 0U)
            {
                esp_log_buffer_hex("Cleaning device:", device->device.address, ESP_BD_ADDR_LEN);
                ResetDeviceEntry(device);
            }
            else
            {
                esp_log_buffer_hex("Device still active:", device->device.address, ESP_BD_ADDR_LEN);
            }
        }

    }
}

static void ManagerIdleWork(void)
{
    if((GetTimestampMs() - lastTimeCleanup) >= DEVICELISTCLEANINTERVAL)
    {
        ESP_LOGI(AppName, "Running cleanup");
        lastTimeCleanup = GetTimestampMs();
        CleanUpDeviceList();

    }
    else if((GetTimestampMs() - lastTimeClosestDevice) >= CLOSESTDEVICEANNOUNCEINTERVAL)
    {
        if((CanSendResponse() == 1U) && (pendingResponse.cmdType == NoOperation))
        {
            pendingResponse.cmdType = GetClosestDevice;
            PrepareClosestDeviceData();
            lastResponseSent = 1U;
            lastTimeClosestDevice = GetTimestampMs();
        }
    }
}

static void ProcessManagerWork(void)
{
    //ESP_LOGI(AppName, "mState: %d", managerState);
    switch(managerState)
    {
        case MgBtMState_ListingAllowedDevices:
        {
            SendListAllowedDevicePacket();
            break;
        }
        case MgBtMState_ListingAllDetectedDevices:
        {
            SendListAllDevicesPacket();
            break;
        }
        default:
        {
            ManagerIdleWork();
        }

    }

    gpio_set_level(GPIO_OUTPUT_IO_0, startLightState & 0x01);
	gpio_set_level(GPIO_OUTPUT_IO_1, startLightState & 0x02);
	gpio_set_level(GPIO_OUTPUT_IO_2, startLightState & 0x04);
    //ESP_LOGI(AppName, "mState2: %d", managerState);
}

static void SendPendingResponse(void)
{
    if(pendingResponse.cmdType != NoOperation)
    {
        SendResponse(&pendingResponse, lastResponseSent);
        if(lastResponseSent == 1U)
        {
            memset(&pendingResponse, 0, sizeof(MGBTCommandData));
            managerState = MgBtMState_Idle;
        }
    }
}

void RunManager(void)
{
    RunCommProto();
    if(CommandAvailable() != 0U)
    {
        ProcessCommand(GetAndClearCommand());
    }
    ProcessManagerWork();
    if(CanSendResponse() != 0U)
    {
        SendPendingResponse();
    }
}


void ProcessScanResult(esp_ble_gap_cb_param_t* scanResult)
{
    /* Search for BLE iBeacon Packet */
    if(esp_ble_is_ibeacon_packet(scanResult->scan_rst.ble_adv, scanResult->scan_rst.adv_data_len))
    {
        esp_ble_ibeacon_t* ibeacon_data = (esp_ble_ibeacon_t*)(scanResult->scan_rst.ble_adv);
        ESP_LOGI(AppName, "iBeacon Found, P1m: %d, R: %d", ibeacon_data->ibeacon_vendor.measured_power, scanResult->scan_rst.rssi);
        esp_log_buffer_hex("Device address:", scanResult->scan_rst.bda, ESP_BD_ADDR_LEN);

        uint16_t index = MAXDEVICES;
        uint16_t firstIndex = MAXDEVICES;

        FindDeviceOrFirstFreeIndex(scanResult->scan_rst.bda, &firstIndex, &index);

        if(index != MAXDEVICES)
        {
            UpdateDeviceData(&deviceList[index], scanResult, ibeacon_data);
        }
        else
        {
            //ESP_LOGI(AppName, "Device is NOT allowed");
            if(GetFilterAllowedDevices() == 0U)
            {
                AddDeviceToListAtIndex(firstIndex, scanResult->scan_rst.bda, 0U, 0);
            }
        }

    }
    /*else
    {
    	ESP_LOGI(AppName, "Found non-iBeacon device:");
    	esp_log_buffer_hex("IBEACON_DEMO: Device address:", scanResult->scan_rst.bda, ESP_BD_ADDR_LEN );
    	ESP_LOGI(AppName, "RSSI of packet:%d dbm", scanResult->scan_rst.rssi);
    }*/
}
