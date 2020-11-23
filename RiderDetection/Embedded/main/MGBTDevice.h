/*
 * MGBTDevice.h
 *
 *  Created on: 21 Nov 2020
 *      Author: cdromke
 */

#ifndef MAIN_MGBTDEVICE_H_
#define MAIN_MGBTDEVICE_H_
#include <stdint.h>
#include "esp_gap_ble_api.h"

typedef struct {
	uint8_t address[ESP_BD_ADDR_LEN];
	int16_t rssi;
	int16_t measuredPower;
} MGBTDevice;

typedef struct {
	MGBTDevice device;
	int16_t lastRssi;
	int16_t averageRssi;
	uint32_t millisFirstSeen;
	uint32_t millisLastSeen;

} MGBTDeviceData;

uint8_t BTAddressEquals(MGBTDevice* otherDevice);

#endif /* MAIN_MGBTDEVICE_H_ */
