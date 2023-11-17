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
#include "CommunicationManager.h"
#include "ConnectedTimestampCollector.h"

#define DISPLAYBUFFERINDEX 1500U
#define COMMPROTOPERIOD 10U

static uint8_t lastCycleButtonState = 0U;
static uint8_t currentBufferDisplayIndex = MAXLAPCOUNT;
static uint32_t lastBufferDisplayChange = 0U;
static Lap* currentlyDisplayedLapFromBuffer = (Lap*)0U;
static DigitalInput* buttonInput = &UserInputs[JmpCommMode];
static uint32_t lastCommunicationRun = 0U;

static void RunCommunication(void)
{
    uint32_t timeStmp = GetSystemTimeStampMs();
    if((timeStmp - lastCommunicationRun) >= COMMPROTOPERIOD)
    {
        lastCommunicationRun = timeStmp;
        RunCommunicationManager();
        uint8_t newConfig = CommMgrGetNewConfig();
        if(newConfig != 0U)
        {
            SetNewConfigMode(newConfig);
        }

    }

}

static Lap* GetPreviousBufferedResult(void)
{
	Lap* retVal = currentlyDisplayedLapFromBuffer;
    if(retVal == &laps[0])
    {
        retVal = &laps[MAXLAPCOUNT - 1U];
    }
    else
    {
    	//Pointer math
        retVal--;
    }

    return retVal;
}

static void ResetBufferedDisplayed(void)
{
	currentBufferDisplayIndex = MAXLAPCOUNT;
	lastBufferDisplayChange = 0;
	currentlyDisplayedLapFromBuffer = (Lap*)0U;

}

static void UpdateDisplayWithBufferedLap(Lap* lapToDisplay)
{
    if(lapToDisplay != (Lap*)0)
    {
        if(lapToDisplay->endTimeStamp != 0U)
        {
            if(GetSystemTimeStampMs() > (lastBufferDisplayChange + DISPLAYBUFFERINDEX))
            {
                UpdateDisplay(GetLapDurationMs(lapToDisplay), LAPTIMERDISPLAYDURATION, DTEA_ClearDisplay);
            }
            else
            {
                UpdateDisplay(GetLapIndex(lapToDisplay) + 1U, 0U, DTEA_ClearDisplay);
            }

        }
        else
        {
        	currentlyDisplayedLapFromBuffer = (Lap*)0U;
        }

        if (currentlyDisplayedLapFromBuffer != lapToDisplay)
        {
        	currentlyDisplayedLapFromBuffer = lapToDisplay;
        	lastBufferDisplayChange = GetSystemTimeStampMs();
        }
    }
}

static Lap* HandleBufferedDisplayButtonRisingEdge(void)
{
	Lap* retVal = currentlyDisplayedLapFromBuffer;
    if(retVal == (Lap*)0U)
    {
    	retVal = GetCurrentLap();
    }

    if(retVal == (Lap*)0U)
    {
		retVal = GetPreviousBufferedResult();
		uint8_t step = 0U;
		while((retVal->endTimeStamp == 0) && (step < MAXLAPCOUNT))
		{
			retVal = GetPreviousBufferedResult();
			step++;
		}
    }

    return retVal;
}

static void ManageDisplayButton(void)
{
    if((GetCurrentLap() != (Lap*)0U) &&
       (IsFirstLap() == 0U) &&
       ((currentlyDisplayedLapFromBuffer != (Lap*)0U) || (InputGetState(buttonInput) == 1U)))
    {
    	uint8_t buttonState = InputGetState(buttonInput);

    	if((lastCycleButtonState == 0U) && (buttonState == 1U))
		{
			UpdateDisplayWithBufferedLap(HandleBufferedDisplayButtonRisingEdge());
		}


        if(buttonState == 1U)
        {
        	if (currentlyDisplayedLapFromBuffer != (Lap*)0U)
        	{
        		lastBufferDisplayChange = GetSystemTimeStampMs();
        	}
        }
        else
        {
            if((lastBufferDisplayChange + LAPTIMERDISPLAYDURATION) < GetSystemTimeStampMs())
            {
            	ResetBufferedDisplayed();
            }
        }

        lastCycleButtonState = buttonState;
    }

    UpdateDisplayWithBufferedLap(currentlyDisplayedLapFromBuffer);
}

static void RunLocalTimer(void)
{
    RunStandAloneTimer();
    if(lapFinished == 1U)
    {
        lapFinished = 0U;
        uint32_t duration = 0U;
        Lap* finishedLap = GetPreviousLap();
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wimplicit-fallthrough="
//Fallthrough is intentional here.
        switch (operationMode)
        {
        	case LaptimerOperation:
        	{
        		duration = LAPTIMERDISPLAYDURATION;
        	}
        	case SingleRunTimerOperation:
			{
				ResetBufferedDisplayed();
				CommMgrSendTimeValue(LastLapTime, GetLapDurationMs(finishedLap));
				UpdateDisplay(GetLapDurationMs(finishedLap), duration, DTEA_ShowRunningTime);
				break;
			}
        	case MultiRunTimerOperation:
        	{
        		CommMgrSendTimeValue(LastLapTime, GetLapDurationMs(finishedLap));
        		UpdateDisplayWithBufferedLap(finishedLap);
        		break;
        	}
        	default:
        	{
        		break;
        	}

        }
    }
#pragma GCC diagnostic push

    if(newRunStarted == 1U)
	{

    	switch (operationMode)
		{
			case LaptimerOperation:
			case SingleRunTimerOperation:
			{
				newRunStarted = 0U;
				ResetRunningDisplayTime(0U);
				break;
			}
			case MultiRunTimerOperation:
			{
				if (currentlyDisplayedLapFromBuffer == (Lap*)0U)
				{
					newRunStarted = 0U;
					UpdateDisplay((GetLapIndex(GetLastStartedLap()) + 1U), DISPLAYBUFFERINDEX, DTEA_ClearDisplay);
					ResetRunningDisplayTime(0U);
				}
				break;
			}
			default:
			{
				break;
			}

		}

	}


    ManageDisplayButton();
}

static void RunConnectedTimer(void)
{
    RunConnectedTimestampCollector();

    if(CommMgrHasNewDisplayUpdate() == 1U)
    {
        UpdateDisplay(CommMgrGetNewDisplayValue(), LAPTIMERDISPLAYDURATION, DTEA_ClearDisplay);
    }

}

void RunRaceTiming(void)
{
    switch(operationMode)
    {
        case SingleRunTimerOperation:
        case LaptimerOperation:
        case MultiRunTimerOperation:
        {
            RunLocalTimer();

            break;
        }
        case ConnectedTimestampCollector:
        {
            RunConnectedTimer();
            break;
        }
        default:
        {
            //Do nothing
            break;
        }
    }

    RunCommunication();

    RunDisplay();
}
