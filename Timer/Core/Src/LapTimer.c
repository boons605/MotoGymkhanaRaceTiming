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
static Lap* currentLap = 0U;
static Lap* previousLap = 0U;
static Lap* lastStartedLap = 0U;
uint8_t lapFinished = 0U;
uint8_t newRunStarted = 0U;


static void SingleSensorLaptimer(void);
static void SingleSensorSingeRuntimer(void);
static void DualSensorSingleRunTimer(void);
static void DualSensorMultiRunTimer(void);
static uint8_t IsLastLap(Lap* lap);
static uint8_t IsLapValid(Lap* lap);
static void InvalidateLap(Lap* lap);

uint8_t GetLapIndex(Lap* lap)
{
	uint8_t retVal = MAXLAPCOUNT;

	if(lap != (Lap*)0U)
	{
		uint8_t index;
		for(index = 0U; index < MAXLAPCOUNT; index++)
		{
			if(lap == &laps[index])
			{
				retVal = index;
			}
		}
	}

	return retVal;
}

Lap* GetCurrentLap(void)
{
    return currentLap;
}

uint8_t IsFirstLap(void)
{
    uint8_t retVal = 0U;

    if((currentLap != (Lap*)0U) && (previousLap == (Lap*)0U))
    {
        retVal = 1U;
    }

    return retVal;
}

uint32_t GetCurrentLapStartTime(void)
{
    if(currentLap != (Lap*)0U)
    {
        return currentLap->startTimeStamp / 10U;
    }

    return 0U;
}

uint32_t GetLapDurationMs(Lap* lap)
{
    uint32_t retVal = 0U;
    if(lap != (Lap*)0U)
    {
        retVal = ((lap->endTimeStamp - lap->startTimeStamp) / 10U);
    }
    return retVal;
}

Lap* GetPreviousLap(void)
{
    return previousLap;
}

static uint8_t IsLapValid(Lap* lap)
{
	uint8_t retVal = 0U;

	if (lap != (Lap*)0U)
	{
		if ((lap->startTimeStamp != 0xFFFFFFFFU) &&
			(lap->endTimeStamp != 0xFFFFFFFFU))
		{
			retVal = 1U;
		}
	}

	return retVal;
}

static void InvalidateLap(Lap* lap)
{
	if (lap != (Lap*)0U)
	{
		lap->startTimeStamp = 0xFFFFFFFFU;
		lap->endTimeStamp = 0xFFFFFFFFU;
	}
}

void InvalidateLapIndex(uint8_t index)
{
	if (index < MAXLAPCOUNT)
	{
		InvalidateLap(&laps[index]);
	}
}

void RunStandAloneTimer(void)
{
    switch(operationMode)
    {
        case LaptimerOperation:
        {
            if(sensorMode == SingleSensor)
            {
                SingleSensorLaptimer();
            }
            break;
        }
        case SingleRunTimerOperation:
        {
            if(sensorMode == SingleSensor)
            {
                SingleSensorSingeRuntimer();
            }
            else if(sensorMode == DualSensor)
            {
                DualSensorSingleRunTimer();
            }
            break;
        }
        case MultiRunTimerOperation:
        {
        	if(sensorMode == DualSensor)
			{
        		DualSensorMultiRunTimer();
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
    if(lap == (&laps[MAXLAPCOUNT - 1]))
    {
        retVal = 1U;
    }

    return retVal;
}

static void FinishCurrentLapAndPrepareNext(SensorTimestamp* timeStamp)
{
    currentLap->endTimeStamp = GetMillisecondsFromTimeStampPPS(timeStamp);
    lapFinished = 1U;
    previousLap = currentLap;
    if(IsLastLap(currentLap) == 0U)
    {
    	//Pointer math!
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

    if(sensorStartStopInterrupt == 1U)
    {
        sensorStartStopInterrupt = 0U;
        GetStartStopSensorTimeStamp(&timeStamp);
        newRunStarted = 1U;

        if(currentLap == (Lap*)0U)
        {
            currentLap = &laps[0];
        }
        else
        {
            FinishCurrentLapAndPrepareNext(&timeStamp);
        }
        currentLap->startTimeStamp = GetMillisecondsFromTimeStampPPS(&timeStamp);
    }

}

static void SingleSensorSingeRuntimer(void)
{
    SensorTimestamp timeStamp;

    if(currentLap == (Lap*)0U)
	{
		currentLap = &laps[0];
		currentLap->startTimeStamp = 0U;
	}

    if(sensorStartStopInterrupt == 1U)
    {
        sensorStartStopInterrupt = 0U;
        GetStartStopSensorTimeStamp(&timeStamp);

		if((currentLap->endTimeStamp == 0U) && (currentLap->startTimeStamp != 0U))
		{
			FinishCurrentLapAndPrepareNext(&timeStamp);
			currentLap->startTimeStamp = 0U;
		}
		else if(currentLap->startTimeStamp == 0U)
		{
			currentLap->startTimeStamp = GetMillisecondsFromTimeStampPPS(&timeStamp);
			currentLap->endTimeStamp = 0U;
			newRunStarted = 1U;
		}
    }
}

static void DualSensorSingleRunTimer(void)
{
    SensorTimestamp timeStamp;

    if(currentLap == (Lap*)0U)
    {
        currentLap = &laps[0];
        currentLap->startTimeStamp = 0U;
    }

    if(sensorStartStopInterrupt == 1U)
    {
        sensorStartStopInterrupt = 0U;
        GetStartStopSensorTimeStamp(&timeStamp);


        if(currentLap->startTimeStamp == 0U)
        {
            currentLap->startTimeStamp = GetMillisecondsFromTimeStampPPS(&timeStamp);
            currentLap->endTimeStamp = 0U;
            newRunStarted = 1U;
        }
    }

    if(sensorStopInterrupt == 1U)
    {
        sensorStopInterrupt = 0U;
        GetStopSensorTimeStamp(&timeStamp);

        if((currentLap->endTimeStamp == 0U) && (currentLap->startTimeStamp != 0U))
        {
            FinishCurrentLapAndPrepareNext(&timeStamp);
            currentLap->startTimeStamp = 0U;
        }

    }
}

static void DualSensorMultiRunTimer(void)
{


}

