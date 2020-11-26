/*
 * MGBTManager.c
 *
 *  Created on: 21 Nov 2020
 *      Author: cdromke
 */

#include "MGBTManager.h"
#include "MGBTCommProto.h"
#include "MGBTDevice.h"

#include "esp_ibeacon_api.h"
#include "esp_log.h"

static const char* AppName = "MGBTManager";

static MGBTDeviceData deviceList[MAXDEVICES] = {0};
static MGBTCommandData pendingResponse = {0};
static uint8_t lastResponseSent = 0U;

void InitManager(void)
{
	InitCommProto();
}

static void FindDeviceOrFirstFreeIndex(uint8_t* address, uint16_t* firstFreeIndex, uint16_t* indexFound)
{

	uint16_t index = 0U;
	MGBTDeviceData emptyDevice = {0};

	(*firstFreeIndex) = MAXDEVICES;
	(*indexFound) = MAXDEVICES;

	while (index < MAXDEVICES)
	{
		if (memcmp(&deviceList[index], &emptyDevice, sizeof(MGBTDeviceData)) != 0U)
		{
			if (BTDeviceAddressEquals(&deviceList[index], address) == 1U)
			{
				(*indexFound) = index;
			}
		}
		else
		{
			if ((*firstFreeIndex) == MAXDEVICES)
			{
				(*firstFreeIndex) = index;
			}
		}
		index++;

		if (((*indexFound) != MAXDEVICES) && ((*firstFreeIndex) != MAXDEVICES))
		{
			index = MAXDEVICES;
		}
	}
}

static void AddDeviceToList(uint8_t* address)
{
	uint16_t firstFreeIndex = MAXDEVICES;
	uint16_t indexFound = MAXDEVICES;

	FindDeviceOrFirstFreeIndex(address, &firstFreeIndex, &indexFound);

	if (indexFound == MAXDEVICES)
	{
		if (firstFreeIndex != MAXDEVICES)
		{
			memcpy(&deviceList[firstFreeIndex].device.address, address, ESP_BD_ADDR_LEN);
			pendingResponse.status = 0U;
			esp_log_buffer_hex("Added device:", address, ESP_BD_ADDR_LEN );
		}
		else
		{
			pendingResponse.status = 0xFFFEU;
		}
	}
	else
	{
		pendingResponse.status = 0xFFFFU;
	}
}

static void RemoveDeviceFromList(uint8_t* address)
{
	uint16_t firstFreeIndex = MAXDEVICES;
	uint16_t indexFound = MAXDEVICES;

	FindDeviceOrFirstFreeIndex(address, &firstFreeIndex, &indexFound);

	if (indexFound != MAXDEVICES)
	{
		memset(&deviceList[indexFound], 0U, sizeof(MGBTDeviceData));
		pendingResponse.status = 0U;
	}
	else
	{
		pendingResponse.status = 0xFFFFU;
	}
}


static void ProcessCommand(MGBTCommandData* command)
{
	switch (command->cmdType)
	{
		case AddAllowedDevice:
		{
			ESP_LOGI(AppName, "Add to allowed devices");
			memcpy(&pendingResponse, command, GetCommandDataSize(command));
			AddDeviceToList(command->data);
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
		case ListAllowedDevices:
		{
			ESP_LOGI(AppName, "List allowed devices");
			memcpy(&pendingResponse, command, GetCommandDataSize(command));
			lastResponseSent = 1U;
			break;
		}
		case ListDetectedDevices:
		{
			ESP_LOGI(AppName, "List detected devices");
			memcpy(&pendingResponse, command, GetCommandDataSize(command));
			lastResponseSent = 1U;
			break;
		}
		case GetClosestDevice:
		{
			ESP_LOGI(AppName, "Get closest device");
			memcpy(&pendingResponse, command, GetCommandDataSize(command));
			lastResponseSent = 1U;
			break;
		}
		default:
		{
			break;
		}

	}
}

static void ProcessManagerWork(void)
{

}

static void SendPendingResponse(void)
{
	if (pendingResponse.cmdType != NoOperation)
	{
		SendResponse(&pendingResponse, lastResponseSent);
		if (lastResponseSent == 1U)
		{
			memset(&pendingResponse, 0, sizeof(MGBTCommandData));
		}
	}
}

void RunManager(void)
{
	RunCommProto();
	if (CommandAvailable() != 0U)
	{
		ProcessCommand(GetAndClearCommand());
	}
	ProcessManagerWork();
	if (CanSendResponse() != 0U)
	{
		SendPendingResponse();
	}
}

void ProcessScanResult(esp_ble_gap_cb_param_t* scanResult)
{
	 /* Search for BLE iBeacon Packet */
	if (esp_ble_is_ibeacon_packet(scanResult->scan_rst.ble_adv, scanResult->scan_rst.adv_data_len)){
		esp_ble_ibeacon_t *ibeacon_data = (esp_ble_ibeacon_t*)(scanResult->scan_rst.ble_adv);
		ESP_LOGI(AppName, "----------iBeacon Found----------");
		esp_log_buffer_hex("IBEACON_DEMO: Device address:", scanResult->scan_rst.bda, ESP_BD_ADDR_LEN );
		/*esp_log_buffer_hex("IBEACON_DEMO: Proximity UUID:", ibeacon_data->ibeacon_vendor.proximity_uuid, ESP_UUID_LEN_128);

		uint16_t major = ENDIAN_CHANGE_U16(ibeacon_data->ibeacon_vendor.major);
		uint16_t minor = ENDIAN_CHANGE_U16(ibeacon_data->ibeacon_vendor.minor);
		ESP_LOGI(AppName, "Major: 0x%04x (%d)", major, major);
		ESP_LOGI(AppName, "Minor: 0x%04x (%d)", minor, minor);
		ESP_LOGI(AppName, "Measured power (RSSI at a 1m distance):%d dbm", ibeacon_data->ibeacon_vendor.measured_power);*/
		ESP_LOGI(AppName, "RSSI of packet:%d dbm", scanResult->scan_rst.rssi);

		uint16_t index = MAXDEVICES;
		uint16_t firstIndex = MAXDEVICES;

		FindDeviceOrFirstFreeIndex(&scanResult->scan_rst.bda, &firstIndex, &index);

		if (index != MAXDEVICES)
		{
			ESP_LOGI(AppName, "Device is allowed");
		}
		else
		{
			ESP_LOGI(AppName, "Device is NOT allowed");
		}

	}
	/*else
	{
		ESP_LOGI(AppName, "Found non-iBeacon device:");
		esp_log_buffer_hex("IBEACON_DEMO: Device address:", scanResult->scan_rst.bda, ESP_BD_ADDR_LEN );
		ESP_LOGI(AppName, "RSSI of packet:%d dbm", scanResult->scan_rst.rssi);
	}*/
}
