/*
 * TimeMgmt.c
 *
 *  Created on: May 9, 2020
 *      Author: r.boonstra
 */
#include "TimeMgmt.h"

volatile SensorTimestamp systemTime;
volatile SensorTimestamp sensorStartStopTimeStamp;
volatile SensorTimestamp sensorStopTimeStamp;

volatile uint8_t sensorStartStopInterrupt = 0U;
volatile uint8_t sensorStopInterrupt = 0U;
volatile uint8_t ppsTick = 0U;


uint32_t GetMillisecondsFromTimeStamp(SensorTimestamp* timeStamp)
{
	return ((timeStamp->timeStampPps*1000U) + timeStamp->ppsOffsetMs);
}
