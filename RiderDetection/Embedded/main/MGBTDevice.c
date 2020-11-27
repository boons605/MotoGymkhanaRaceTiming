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

uint8_t BTDeviceEquals(MGBTDeviceData* device, MGBTDeviceData* otherDevice)
{
	uint8_t retVal = 0U;
	if (device == otherDevice)
	{

	}
	else if ((device != (MGBTDeviceData*)0) && (otherDevice != (MGBTDeviceData*)0))
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
	if ((address1 != (uint8_t*)0) && (address2 != (uint8_t*)0))
	{
		if (memcmp(address1, address2, ESP_BD_ADDR_LEN) == 0)
		{
			retVal = 1U;
		}
	}
	return retVal;
}

uint8_t BTDeviceAddressEquals(MGBTDeviceData* device, uint8_t* address)
{
	uint8_t retVal = 0U;
	if ((device != (MGBTDeviceData*)0) && (address != (uint8_t*)0))
	{
		retVal = BTAddressEquals(device->device.address, address);
	}

	return retVal;
}

//Get Distance in 0.1m
//Formula found here: https://iotandelectronics.wordpress.com/2016/10/07/how-to-calculate-distance-from-the-rssi-value-of-the-ble-beacon/
uint16_t GetDistance(MGBTDeviceData* device)
{
	uint16_t retVal = 0U;
	if (device != (MGBTDeviceData*)0)
	{
		if (device->averageRssi > device->device.measuredPower)
		{
			retVal = 10U;
		}
		else
		{
			double exp = (double)(device->device.measuredPower - device->averageRssi);
			exp /= (double)(10*DISTANCEENVFACTOR);

			double distance = pow(10, exp) * 10;
			retVal = (uint16_t)distance;
		}

	}
	return retVal;
}

uint8_t IsDeviceActive(MGBTDeviceData* device)
{
	uint8_t retVal;
	if (device != (MGBTDeviceData*)0)
	{
		if ((device->millisLastSeen > 0U) &&
			((GetTimestampMs() - device->millisLastSeen) < ACTIVEDEVICETIMEOUT) &&
			(device->averageRssi > ACTIVEDEVICEMINRSSI))
		{
			retVal = 1;
		}

	}
	return retVal;
}

void UpdateDeviceData(MGBTDeviceData* device, esp_ble_gap_cb_param_t* scanResult, esp_ble_ibeacon_t *ibeacon_data)
{
	if ((device != (MGBTDeviceData*)0) && (scanResult != (esp_ble_gap_cb_param_t*)0) && (ibeacon_data != (esp_ble_ibeacon_t*)0))
	{
		device->lastRssi = (uint16_t)scanResult->scan_rst.rssi;
		device->averageRssi = (((device->averageRssi * (RSSISAMPLES-1)) + device->lastRssi) / RSSISAMPLES);
		device->device.rssi = device->averageRssi;

		if (device->millisFirstSeen == 0U)
		{
			device->millisFirstSeen = GetTimestampMs();
		}
		device->millisLastSeen = GetTimestampMs();

		device->device.measuredPower = ibeacon_data->ibeacon_vendor.measured_power;
	}
}
