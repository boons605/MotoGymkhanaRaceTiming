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
#include "esp_ibeacon_api.h"

#define RSSISAMPLES 4
#define ACTIVEDEVICETIMEOUT 10000
#define MAXDISTEXPONENT 0.85
#define DISTANCEENVFACTOR 2

typedef struct
{
    uint8_t address[ESP_BD_ADDR_LEN];
    int16_t rssi;
    int16_t measuredPower;
    uint16_t distance;
} MGBTDevice;

typedef struct
{
    MGBTDevice device;
    int16_t lastRssi;
    int16_t averageRssi;
    uint32_t millisFirstSeen;
    uint32_t millisLastSeen;
    uint8_t allowed;
    double lastDistanceExponent;


} MGBTDeviceData;

uint8_t BTDeviceEquals(MGBTDeviceData* device, MGBTDeviceData* otherDevice);
uint8_t BTAddressEquals(uint8_t* address1, uint8_t* address2);
uint8_t BTDeviceAddressEquals(MGBTDeviceData* device, uint8_t* address);
uint16_t GetDistance(MGBTDeviceData* device);
void UpdateDeviceData(MGBTDeviceData* device, esp_ble_gap_cb_param_t* scanResult, esp_ble_ibeacon_t* ibeacon_data);
uint8_t IsDeviceActive(MGBTDeviceData* device);
uint8_t IsDeviceEntryEmpty(MGBTDeviceData* device);
void ClearDeviceEntry(MGBTDeviceData* device);

#endif /* MAIN_MGBTDEVICE_H_ */
