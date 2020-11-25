/*
 * MGBTDevice.c
 *
 *  Created on: 21 Nov 2020
 *      Author: cdromke
 */


#include "MGBTDevice.h"
#include "esp_gap_bt_api.h"

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
uint16_t GetDistance(MGBTDeviceData* device)
{

}
