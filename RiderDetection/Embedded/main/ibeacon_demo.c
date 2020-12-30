/*
   This example code is in the Public Domain (or CC0 licensed, at your option.)

   Unless required by applicable law or agreed to in writing, this
   software is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR
   CONDITIONS OF ANY KIND, either express or implied.
*/



/****************************************************************************
*
* This file is for iBeacon demo. It supports both iBeacon sender and receiver
* which is distinguished by macros IBEACON_SENDER and IBEACON_RECEIVER,
*
* iBeacon is a trademark of Apple Inc. Before building devices which use iBeacon technology,
* visit https://developer.apple.com/ibeacon/ to obtain a license.
*
****************************************************************************/

#include <stdint.h>
#include <string.h>
#include <stdbool.h>
#include <stdio.h>
#include "nvs_flash.h"

#include "esp_bt.h"
#include "esp_gap_ble_api.h"
#include "esp_gattc_api.h"
#include "esp_gatt_defs.h"
#include "esp_bt_main.h"
#include "esp_bt_defs.h"
#include "esp_log.h"
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"
#include "string.h"
#include "MGBTManager.h"

static const char* AppName = "MGBTDetectMain";

///Declare static functions
static void esp_gap_cb(esp_gap_ble_cb_event_t event, esp_ble_gap_cb_param_t* param);


static esp_ble_scan_params_t ble_scan_params =
{
    .scan_type              = BLE_SCAN_TYPE_ACTIVE,
    .own_addr_type          = BLE_ADDR_TYPE_PUBLIC,
    .scan_filter_policy     = BLE_SCAN_FILTER_ALLOW_ALL,
    .scan_interval          = 0x50,
    .scan_window            = 0x30,
    .scan_duplicate         = BLE_SCAN_DUPLICATE_DISABLE
};




static void esp_gap_cb(esp_gap_ble_cb_event_t event, esp_ble_gap_cb_param_t* param)
{
    esp_err_t err;

    switch(event)
    {
        case ESP_GAP_BLE_SCAN_PARAM_SET_COMPLETE_EVT:
        {
            //the unit of the duration is second, 0 means scan permanently
            uint32_t duration = 0;
            esp_ble_gap_start_scanning(duration);
            break;
        }
        case ESP_GAP_BLE_SCAN_START_COMPLETE_EVT:
            //scan start complete event to indicate scan start successfully or failed
            if((err = param->scan_start_cmpl.status) != ESP_BT_STATUS_SUCCESS)
            {
                ESP_LOGE(AppName, "Scan start failed: %s", esp_err_to_name(err));
            }
            break;
        case ESP_GAP_BLE_ADV_START_COMPLETE_EVT:
            //adv start complete event to indicate adv start successfully or failed
            if((err = param->adv_start_cmpl.status) != ESP_BT_STATUS_SUCCESS)
            {
                ESP_LOGE(AppName, "Adv start failed: %s", esp_err_to_name(err));
            }
            break;
        case ESP_GAP_BLE_SCAN_RESULT_EVT:
        {
            esp_ble_gap_cb_param_t* scan_result = (esp_ble_gap_cb_param_t*)param;
            switch(scan_result->scan_rst.search_evt)
            {
                case ESP_GAP_SEARCH_INQ_RES_EVT:
                    ProcessScanResult(scan_result);
                    break;
                default:
                    break;
            }
            break;
        }

        case ESP_GAP_BLE_SCAN_STOP_COMPLETE_EVT:
            if((err = param->scan_stop_cmpl.status) != ESP_BT_STATUS_SUCCESS)
            {
                ESP_LOGE(AppName, "Scan stop failed: %s", esp_err_to_name(err));
            }
            else
            {
                ESP_LOGI(AppName, "Stop scan successfully");
            }
            break;

        case ESP_GAP_BLE_ADV_STOP_COMPLETE_EVT:
            if((err = param->adv_stop_cmpl.status) != ESP_BT_STATUS_SUCCESS)
            {
                ESP_LOGE(AppName, "Adv stop failed: %s", esp_err_to_name(err));
            }
            else
            {
                ESP_LOGI(AppName, "Stop adv successfully");
            }
            break;

        default:
            break;
    }
}


static void ble_ibeacon_appRegister(void)
{
    esp_err_t status;

    ESP_LOGI(AppName, "register callback");

    //register the scan callback function to the gap module
    if((status = esp_ble_gap_register_callback(esp_gap_cb)) != ESP_OK)
    {
        ESP_LOGE(AppName, "gap register error: %s", esp_err_to_name(status));
        return;
    }

}

static void ble_ibeacon_init(void)
{
    esp_bluedroid_init();
    esp_bluedroid_enable();
    ble_ibeacon_appRegister();
}

static void rx_task(void* arg)
{
    const TickType_t xFrequency = 10 / portTICK_PERIOD_MS;
    TickType_t lastWakeTime = xTaskGetTickCount();

    while(1)
    {
        RunManager();
        vTaskDelayUntil(&lastWakeTime, xFrequency);
    }
}

void app_main(void)
{
    ESP_ERROR_CHECK(nvs_flash_init());
    ESP_ERROR_CHECK(esp_bt_controller_mem_release(ESP_BT_MODE_CLASSIC_BT));
    esp_bt_controller_config_t bt_cfg = BT_CONTROLLER_INIT_CONFIG_DEFAULT();
    esp_bt_controller_init(&bt_cfg);
    esp_bt_controller_enable(ESP_BT_MODE_BLE);

    ble_ibeacon_init();
    InitManager();
    /* set scan parameters */

    esp_ble_gap_set_scan_params(&ble_scan_params);

    xTaskCreate(rx_task, "uart_rx_task", 4096, NULL, configMAX_PRIORITIES, NULL);
}

