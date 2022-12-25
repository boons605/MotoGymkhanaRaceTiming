/*
 * TimeMgmt.c
 *
 *  Created on: May 9, 2020
 *      Author: r.boonstra
 */
#include "TimeMgmt.h"
#include "stm32f1xx_ll_exti.h"
#include <string.h>

volatile SensorTimestamp systemTime;
volatile SensorTimestamp sensorStartStopTimeStamp;
volatile SensorTimestamp sensorStopTimeStamp;

volatile uint8_t sensorStartStopInterrupt = 0U;
volatile uint8_t sensorStopInterrupt = 0U;
volatile uint8_t ppsTick = 0U;

uint32_t GetMillisecondsFromTimeStampPPS(SensorTimestamp* timeStamp)
{
    return ((timeStamp->timeStampPps * 10000U) + timeStamp->ppsOffset100us);
}

uint32_t GetMillisecondsFromTimeStamp(SensorTimestamp* timeStamp)
{
    return (timeStamp->timeStamp100us / 10U);
}

uint32_t GetSystemTimeStampMs(void)
{
    return systemTime.timeStamp100us / 10U;
}
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wdiscarded-qualifiers"
//We know an willingly ignore the volatile qualifier, as we temporarily
//disable interrupts that might cause the change that justifies the volatile
//keyword.
void GetStartStopSensorTimeStamp(SensorTimestamp* copy)
{
    LL_EXTI_DisableRisingTrig_0_31(LL_EXTI_LINE_8);
    memcpy(copy, &sensorStartStopTimeStamp, sizeof(SensorTimestamp));
    LL_EXTI_EnableRisingTrig_0_31(LL_EXTI_LINE_8);
}

void GetStopSensorTimeStamp(SensorTimestamp* copy)
{
    LL_EXTI_DisableRisingTrig_0_31(LL_EXTI_LINE_11);
    memcpy(copy, &sensorStopTimeStamp, sizeof(SensorTimestamp));
    LL_EXTI_EnableRisingTrig_0_31(LL_EXTI_LINE_11);
}
#pragma GCC diagnostic pop
