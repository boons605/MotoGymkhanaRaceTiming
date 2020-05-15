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

Lap laps[MAXLAPCOUNT] = { 0 };
Lap* currentLap = 0U;
Lap* previousLap = 0U;
uint8_t lapFinished = 0U;
uint8_t newRunStarted = 0U;

static void SingleSensorLaptimer(void);
static void SingleSensorSingeRuntimer(void);
static void DualSensorSingleRunTimer(void);
static uint8_t IsLastLap(Lap* lap);

void RunStandAloneTimer(void)
{
	switch (operationMode)
	{
		case LaptimerOperation:
		{
			if (sensorMode == SingleSensor)
			{
				SingleSensorLaptimer();
			}
			break;
		}
		case SingleRunTimerOperation:
		{
			if (sensorMode == SingleSensor)
			{
				SingleSensorSingeRuntimer();
			}
			else if (sensorMode == DualSensor)
			{
				DualSensorSingleRunTimer();
			}
			break;
		}
		default:
		{
			break;
		}
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

static void FinishCurrentLapAndPrepareNext(SensorTimestamp* timeStamp)
{
	currentLap->endTimeStamp = GetMillisecondsFromTimeStamp(timeStamp);
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
			FinishCurrentLapAndPrepareNext(&timeStamp);
		}
		currentLap->startTimeStamp = GetMillisecondsFromTimeStamp(&timeStamp);
	}

}

static void SingleSensorSingeRuntimer(void)
{
	SensorTimestamp timeStamp;

	if (sensorStartStopInterrupt == 1U)
	{
		sensorStartStopInterrupt = 0U;
		GetStartStopSensorTimeStamp(&timeStamp);

		if (currentLap == (Lap*)0U)
		{
			currentLap = &laps[0];
			currentLap->startTimeStamp = GetMillisecondsFromTimeStamp(&timeStamp);
			currentLap->endTimeStamp = 0U;
		}
		else
		{
			if ((currentLap->endTimeStamp == 0U) && (currentLap->startTimeStamp != 0U))
			{
				FinishCurrentLapAndPrepareNext(&timeStamp);
				currentLap->startTimeStamp = 0U;
			}
			else if (currentLap->startTimeStamp == 0U)
			{
				currentLap->startTimeStamp = GetMillisecondsFromTimeStamp(&timeStamp);
				currentLap->endTimeStamp = 0U;
				newRunStarted = 1U;
			}
		}

	}

}

static void DualSensorSingleRunTimer(void)
{
	SensorTimestamp timeStamp;

	if (currentLap == (Lap*)0U)
	{
		currentLap = &laps[0];
		currentLap->startTimeStamp = 0U;
	}

	if (sensorStartStopInterrupt == 1U)
	{
		sensorStartStopInterrupt = 0U;
		GetStartStopSensorTimeStamp(&timeStamp);


		if (currentLap->startTimeStamp == 0U)
		{
			currentLap->startTimeStamp = GetMillisecondsFromTimeStamp(&timeStamp);
			currentLap->endTimeStamp = 0U;
			newRunStarted = 1U;
		}
	}

	if (sensorStopInterrupt == 1U)
	{
		sensorStopInterrupt = 0U;
		GetStopSensorTimeStamp(&timeStamp);

		if ((currentLap->endTimeStamp == 0U) && (currentLap->startTimeStamp != 0U))
		{
			FinishCurrentLapAndPrepareNext(&timeStamp);
			currentLap->startTimeStamp = 0U;
		}

	}
}

