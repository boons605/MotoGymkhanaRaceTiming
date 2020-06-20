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

uint8_t GetCurrentLapIndex(void)
{
    uint8_t retVal = MAXLAPCOUNT;

    if(currentLap != (Lap*)0U)
    {
        uint8_t index;
        for(index = 0U; index < MAXLAPCOUNT; index++)
        {
            if(currentLap == &laps[index])
            {
                retVal = index;
            }
        }
    }

    return retVal;

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

uint32_t GetLapTimestampMs(Lap* lap)
{
    uint32_t retVal = 0U;
    if(lap != (Lap*)0U)
    {
        retVal = ((lap->endTimeStamp - lap->startTimeStamp) / 10U);
    }
    return retVal;
}

uint32_t GetPreviousLapTimeMs(void)
{
    return GetLapTimestampMs(previousLap);
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

    if(sensorStartStopInterrupt == 1U)
    {
        sensorStartStopInterrupt = 0U;
        GetStartStopSensorTimeStamp(&timeStamp);

        if(currentLap == (Lap*)0U)
        {
            currentLap = &laps[0];
            newRunStarted = 1U;
            currentLap->startTimeStamp = GetMillisecondsFromTimeStampPPS(&timeStamp);
            currentLap->endTimeStamp = 0U;
        }
        else
        {
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

