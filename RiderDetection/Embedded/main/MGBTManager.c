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

void InitManager(void)
{
	InitCommProto();
}

void RunManager(void)
{
	RunCommProto();
}

void ProcessScanResult(esp_ble_gap_cb_param_t* scanResult)
{
	 /* Search for BLE iBeacon Packet */
	if (esp_ble_is_ibeacon_packet(scanResult->scan_rst.ble_adv, scanResult->scan_rst.adv_data_len)){
		esp_ble_ibeacon_t *ibeacon_data = (esp_ble_ibeacon_t*)(scanResult->scan_rst.ble_adv);
		ESP_LOGI(AppName, "----------iBeacon Found----------");
		esp_log_buffer_hex("IBEACON_DEMO: Device address:", scanResult->scan_rst.bda, ESP_BD_ADDR_LEN );
		esp_log_buffer_hex("IBEACON_DEMO: Proximity UUID:", ibeacon_data->ibeacon_vendor.proximity_uuid, ESP_UUID_LEN_128);

		uint16_t major = ENDIAN_CHANGE_U16(ibeacon_data->ibeacon_vendor.major);
		uint16_t minor = ENDIAN_CHANGE_U16(ibeacon_data->ibeacon_vendor.minor);
		ESP_LOGI(AppName, "Major: 0x%04x (%d)", major, major);
		ESP_LOGI(AppName, "Minor: 0x%04x (%d)", minor, minor);
		ESP_LOGI(AppName, "Measured power (RSSI at a 1m distance):%d dbm", ibeacon_data->ibeacon_vendor.measured_power);
		ESP_LOGI(AppName, "RSSI of packet:%d dbm", scanResult->scan_rst.rssi);
	}
	else
	{
		ESP_LOGI(AppName, "Found non-iBeacon device:");
		esp_log_buffer_hex("IBEACON_DEMO: Device address:", scanResult->scan_rst.bda, ESP_BD_ADDR_LEN );
		ESP_LOGI(AppName, "RSSI of packet:%d dbm", scanResult->scan_rst.rssi);
	}
}
