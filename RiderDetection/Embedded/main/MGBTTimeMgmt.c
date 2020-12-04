/*
 * MGBTTimeMgmt.c
 *
 *  Created on: 21 Nov 2020
 *      Author: cdromke
 */

#include "MGBTTimeMgmt.h"
#include "esp_timer.h"

uint32_t GetTimestampMs(void)
{
    return (uint32_t)(esp_timer_get_time() / 1000U);
}
