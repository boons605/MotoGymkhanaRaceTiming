/*
 * RaceTiming.c
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#include "Configuration.h"
#include "RaceTiming.h"
#include "LapTimer.h"
#include "Display.h"
#include "Inputs.h"
#include "TimeMgmt.h"
#include "MGBTCommProto.h"
#include "CommunicationManager.h"

#define DISPLAYBUFFERINDEX 750U
#define COMMPROTOPERIOD 10U

static uint8_t displayBufferResult = 0U;
static uint8_t lastCycleButtonState = 0U;
static uint8_t currentBufferDisplayIndex = MAXLAPCOUNT;
static uint32_t lastBufferDisplayChange = 0U;
static uint32_t lastTimeButtonHigh = 0U;
static DigitalInput* buttonInput = &UserInputs[JmpCommMode];
static uint32_t lastCommunicationRun = 0U;

static void RunCommunication(void)
{
    uint32_t timeStmp = GetSystemTimeStampMs();
    if((timeStmp - lastCommunicationRun) > COMMPROTOPERIOD)
    {
        lastCommunicationRun = timeStmp;
        RunCommProto();
        RunCommunicationManager();
    }

}

static uint8_t GetPreviousBufferedResult(void)
{
    uint8_t retVal;
    if(currentBufferDisplayIndex == 0U)
    {
        retVal = MAXLAPCOUNT - 1U;
    }
    else
    {
        retVal = currentBufferDisplayIndex - 1U;
    }

    return retVal;
}

static void UpdateDisplayWithBufferedLap(Lap* currentDisplayedLap)
{
    if(currentDisplayedLap != (Lap*)0)
    {
        if(currentDisplayedLap->endTimeStamp != 0U)
        {
            if(GetSystemTimeStampMs() > (lastBufferDisplayChange + DISPLAYBUFFERINDEX))
            {
                UpdateDisplay(GetLapTimestampMs(currentDisplayedLap), LAPTIMERDISPLAYDURATION);
            }
            else
            {
                UpdateDisplay(currentBufferDisplayIndex + 1, 0U);
            }

        }
        else
        {
            displayBufferResult = 0U;
        }
    }
}

static void HandleBufferedDisplayButtonRisingEdge(void)
{
    if(currentBufferDisplayIndex == MAXLAPCOUNT)
    {
        currentBufferDisplayIndex = GetCurrentLapIndex();
    }

    currentBufferDisplayIndex = GetPreviousBufferedResult();
    uint8_t step = 0U;
    while((laps[currentBufferDisplayIndex].endTimeStamp == 0) && (step < MAXLAPCOUNT))
    {
        currentBufferDisplayIndex = GetPreviousBufferedResult();
        step++;
    }
    lastBufferDisplayChange = GetSystemTimeStampMs();
}

static void DisplayBufferResult(void)
{
    if((lastCycleButtonState == 0U) && (InputGetState(buttonInput) == 1U))
    {
        HandleBufferedDisplayButtonRisingEdge();
    }

    UpdateDisplayWithBufferedLap(&laps[currentBufferDisplayIndex]);

}

static void ManageDisplayButton(void)
{
    if((GetCurrentLapIndex() != MAXLAPCOUNT) &&
       (IsFirstLap() == 0U) &&
       ((displayBufferResult == 1U) || (InputGetState(buttonInput) == 1U)))
    {
        DisplayBufferResult();

        if(InputGetState(buttonInput) == 1U)
        {
            displayBufferResult = 1U;
            lastTimeButtonHigh = GetSystemTimeStampMs();
        }
        else
        {
            if((lastTimeButtonHigh + LAPTIMERDISPLAYDURATION) < GetSystemTimeStampMs())
            {
                displayBufferResult = 0U;
                currentBufferDisplayIndex = MAXLAPCOUNT;
            }
        }

        lastCycleButtonState = InputGetState(buttonInput);
    }
}

void RunRaceTiming(void)
{
    switch(operationMode)
    {
        case SingleRunTimerOperation:
        case LaptimerOperation:
        {
            RunStandAloneTimer();
            if(lapFinished == 1U)
            {
                lapFinished = 0U;
                uint32_t duration = 0U;
                if(operationMode == LaptimerOperation)
                {
                    duration = LAPTIMERDISPLAYDURATION;
                }
                CommMgrSendTimeValue(LastLapTime, GetPreviousLapTimeMs());
                UpdateDisplay(GetPreviousLapTimeMs(), duration);
                displayBufferResult = 0U;
            }

            if(newRunStarted == 1U)
            {
                newRunStarted = 0U;
                ResetRunningDisplayTime(0U);
            }

            ManageDisplayButton();

            break;
        }
        case ConnectedTimestampCollector:
        {
            RunConnectedTimestampCollector();
            break;
        }
        default:
        {
            //Do nothing
            break;
        }
    }



    RunDisplay();
}
