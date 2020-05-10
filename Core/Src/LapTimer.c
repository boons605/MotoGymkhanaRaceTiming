/*
 * LapTimer.c
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#ifndef SRC_LAPTIMER_C_
#define SRC_LAPTIMER_C_



#endif /* SRC_LAPTIMER_C_ */

#include "LapTimer.h"
#include "Configuration.h"
#include "TimeMgmt.h"

Lap laps[MAXLAPCOUNT];
Lap* currentLap = 0U;
Lap* previousLap = 0U;
uint8_t lapFinished = 0U;

static void SingleSensorLaptimer(void);
static uint8_t IsLastLap(Lap* lap);

void RunLapTimer(void)
{
	if (sensorMode == SingleSensor)
	{
		SingleSensorLaptimer();
	}
}

static uint8_t IsLastLap(Lap* lap)
{
	uint8_t retVal = 0U;
	if (lap == (&laps[MAXLAPCOUNT-1]))
	{
		retVal = 1U;
	}

	return retVal;
}

static void SingleSensorLaptimer(void)
{
	SensorTimestamp timeStamp;

	if (sensorStartStopInterrupt == 1U)
	{
		sensorStartStopInterrupt = 0U;
		GetStartStopSensorTimeStamp(&timeStamp);

		if (currentLap == (Lap*)0U)
		{
			currentLap = &laps[0];
		}
		else
		{
			currentLap->endTimeStamp = GetMillisecondsFromTimeStamp(&timeStamp);
			lapFinished = 1U;
			previousLap = currentLap;
			if (IsLastLap(currentLap) == 0U)
			{
				currentLap++;
			}
			else
			{
				currentLap = &laps[0];
			}
		}
		currentLap->startTimeStamp = GetMillisecondsFromTimeStamp(&timeStamp);
	}

}

