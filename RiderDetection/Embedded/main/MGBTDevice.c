/*
 * MGBTDevice.c
 *
 *  Created on: 21 Nov 2020
 *      Author: cdromke
 */


#include "MGBTDevice.h"
#include "esp_gap_bt_api.h"
#include "MGBTTimeMgmt.h"
#include <string.h>
#include <math.h>
#include "esp_log.h"

static const MGBTDeviceData emptyDevice = {0};

static const char* AppName = "Device struct manager";

uint8_t BTDeviceEquals(MGBTDeviceData* device, MGBTDeviceData* otherDevice)
{
    uint8_t retVal = 0U;
    if(device == otherDevice)
    {
        retVal = 1U;
    }
    else if((device != (MGBTDeviceData*)0) && (otherDevice != (MGBTDeviceData*)0))
    {
        retVal = BTAddressEquals(device->device.address, otherDevice->device.address);
    }
    else
    {
        //Do nothing
    }
    return retVal;
}

uint8_t BTAddressEquals(uint8_t* address1, uint8_t* address2)
{
    uint8_t retVal = 0U;
    if((address1 != (uint8_t*)0) && (address2 != (uint8_t*)0))
    {
        if(memcmp(address1, address2, ESP_BD_ADDR_LEN) == 0)
        {
            retVal = 1U;
        }
    }
    return retVal;
}

uint8_t BTDeviceAddressEquals(MGBTDeviceData* device, uint8_t* address)
{
    uint8_t retVal = 0U;
    if((device != (MGBTDeviceData*)0) && (address != (uint8_t*)0))
    {
        retVal = BTAddressEquals(device->device.address, address);
    }

    return retVal;
}


//Exponent > 3 is used as error value, as this would mean more than 1000m, which is well outside the
//the maximum range of BLE, which is about 100m.
static double GetDistancingExponent(MGBTDeviceData* device)
{
    double retVal = 3.1;

    if(device != (MGBTDeviceData*)0)
    {
        retVal = (double)(device->device.measuredPower - device->averageRssi);
        retVal /= (double)(10 * DISTANCEENVFACTOR);
    }

    return retVal;
}

//Get Distance in 0.1m
//Formula found here: https://iotandelectronics.wordpress.com/2016/10/07/how-to-calculate-distance-from-the-rssi-value-of-the-ble-beacon/
//Return value of > 1500 is considered error, as the max range of BLE is about 100m.
uint16_t GetDistance(MGBTDeviceData* device)
{
    uint16_t retVal = 10000U;
    if(device != (MGBTDeviceData*)0)
    {
        double exp = device->lastDistanceExponent;
        double absExp = fabs(exp);
        if(absExp < 3.0)
        {
            double distance = pow(10, absExp);
            if(exp > 0)
            {
                retVal = (uint16_t)(distance * 10.0);
            }
            else
            {
                retVal = (uint16_t)((1.0 / distance) * 10.0);
            }
        }
    }
    return retVal;
}

uint8_t IsDeviceActive(MGBTDeviceData* device)
{
    uint8_t retVal = 0U;
    if(device != (MGBTDeviceData*)0)
    {
        ESP_LOGI(AppName, "Last seen %d, time since last seen %d, distExp %f", device->millisLastSeen, (GetTimestampMs() - device->millisLastSeen), device->lastDistanceExponent);
        if((device->millisLastSeen > 0U) &&
           ((GetTimestampMs() - device->millisLastSeen) < ACTIVEDEVICETIMEOUT) &&
           (device->lastDistanceExponent < MAXDISTEXPONENT))
        {
            retVal = 1;
        }

    }
    return retVal;
}

void UpdateDeviceData(MGBTDeviceData* device, esp_ble_gap_cb_param_t* scanResult, esp_ble_ibeacon_t* ibeacon_data)
{
    if((device != (MGBTDeviceData*)0) && (scanResult != (esp_ble_gap_cb_param_t*)0) && (ibeacon_data != (esp_ble_ibeacon_t*)0))
    {
        device->device.measuredPower = ibeacon_data->ibeacon_vendor.measured_power - device->device.measuredPowerCorrection;

        device->lastRssi = (int16_t)scanResult->scan_rst.rssi;
        if(device->averageRssi > (device->device.measuredPower / 2))
        {
            device->averageRssi = device->lastRssi;
        }
        else
        {
            device->averageRssi = (((device->averageRssi * (RSSISAMPLES - 1)) + device->lastRssi) / RSSISAMPLES);
        }
        device->device.rssi = device->averageRssi;

        if(device->millisFirstSeen == 0U)
        {
            device->millisFirstSeen = GetTimestampMs();
        }
        device->millisLastSeen = GetTimestampMs();


        device->lastDistanceExponent = GetDistancingExponent(device);
    }
}

uint8_t IsDeviceEntryEmpty(MGBTDeviceData* device)
{
    uint8_t retVal = 0U;
    if(device != (MGBTDeviceData*)0)
    {
        if(memcmp(device, &emptyDevice, sizeof(MGBTDeviceData)) == 0U)
        {
            retVal = 1U;
        }
    }
    return retVal;

}

void ClearDeviceEntry(MGBTDeviceData* device)
{
    if(device != (MGBTDeviceData*)0)
    {
        memset(device, 0U, sizeof(MGBTDeviceData));
    }
}

void ResetDeviceEntry(MGBTDeviceData* device)
{
    if(device != (MGBTDeviceData*)0)
    {
        if(device->allowed == 0U)
        {
            ClearDeviceEntry(device);
        }
        else
        {
            uint8_t address[ESP_BD_ADDR_LEN] = {0};
            uint16_t correction = device->device.measuredPowerCorrection;
            memcpy(address, device->device.address, ESP_BD_ADDR_LEN);
            ClearDeviceEntry(device);
            memcpy(device->device.address, address, ESP_BD_ADDR_LEN);
            device->device.measuredPowerCorrection = correction;
            device->allowed = 1U;
        }
    }
}
