/*
 * Display.c
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */
#include "Configuration.h"
#include "Display.h"
#include "TimeMgmt.h"
#include "Max7219Display.h"
#include "Max7219DLDWDisplay.h"


static uint32_t CalculateMinutesComponent(uint32_t milliSeconds);
static uint32_t CalculateSecondsComponent(uint32_t milliSeconds);
static uint32_t CalculateMillisecondsComponent(uint32_t milliSeconds);
static void UpdateDisplayedTime(uint32_t milliseconds, uint8_t cutOffLastDigits);

static uint32_t displayResultUntil = 0U;
static uint8_t permanentResultDisplay = 0U;
static uint32_t displayedResult = 0U;
static uint32_t runningTimeStartTime = 0U;
static uint32_t lastDisplayUpdate = 0U;

static uint8_t displayConfig = 1U;

void UpdateDisplay(uint32_t newTimeInMs, uint32_t displayDurationInMs)
{
    if(displayedResult != newTimeInMs)
    {
        if(displayDurationInMs > 0U)
        {
            displayResultUntil = GetSystemTimeStampMs() + displayDurationInMs;
            permanentResultDisplay = 0U;
        }
        else
        {
            permanentResultDisplay = 1U;
        }

        displayedResult = newTimeInMs;
        //Force display update
        lastDisplayUpdate = 0U;
        displayConfig = 0U;
    }

}

void ResetRunningDisplayTime(uint32_t startTime)
{
    if(startTime == 0U)
    {
        runningTimeStartTime = GetSystemTimeStampMs();
    }
    else
    {
        runningTimeStartTime = startTime;
    }
    permanentResultDisplay = 0U;
    displayConfig = 0U;
}

void RunDisplay(void)
{
    uint32_t timeStamp = GetSystemTimeStampMs();

    if((timeStamp < displayResultUntil) ||
       (permanentResultDisplay == 1U))
    {
        if(((lastDisplayUpdate + 500U) < timeStamp))
        {
            UpdateDisplayedTime(displayedResult, 0U);

        }
    }
    else
    {
        if((lastDisplayUpdate + 100U) < timeStamp)
        {
            UpdateDisplayedTime(timeStamp - runningTimeStartTime, 1U);
        }
    }

    if(displayLines == 2U)
    {
        RunMax7219DLDWDisplay();
    }
    else
    {
        RunMax7219Display();
    }

}


static void UpdateDisplayedTime(uint32_t milliseconds, uint8_t cutOffLastDigits)
{
    uint32_t bcdDisplayData = 0U;

    bcdDisplayData |= CalculateMinutesComponent(milliseconds) << 20;
    bcdDisplayData |= CalculateSecondsComponent(milliseconds) << 12;
    bcdDisplayData |= CalculateMillisecondsComponent(milliseconds);
    if(cutOffLastDigits == 1U)
    {
        bcdDisplayData &= 0x00FFFF00;
        bcdDisplayData |= 0x000000CC;
    }

    if(displayConfig == 1U)
    {
        bcdDisplayData = GetConfigBCDDisplay();
    }

    if(displayLines == 2U)
    {
        UpdateMax7219DLDWDisplay(bcdDisplayData);
    }
    else
    {
        UpdateMax7219Display(bcdDisplayData);
    }
    lastDisplayUpdate = GetSystemTimeStampMs();
}

static uint32_t CalculateMinutesComponent(uint32_t milliSeconds)
{
    return milliSeconds / 60000U;
}

static uint32_t CalculateSecondsComponent(uint32_t milliSeconds)
{
    uint8_t retVal = (milliSeconds % 60000U) / 1000U;
    uint8_t calc = retVal;
    calc /= 10U;
    retVal -= (calc * 10U);
    retVal += (calc << 4U);

    return retVal;
}

static uint32_t CalculateMillisecondsComponent(uint32_t milliSeconds)
{
    uint16_t retVal = ((milliSeconds % 60000U) % 1000U);
    uint16_t calc = retVal;
    uint16_t calc2 = retVal;

    calc2 /= 100U;
    retVal -= (calc2 * 100U);
    calc = retVal;
    calc2 <<= 8;
    calc /= 10U;
    retVal -= (calc * 10U);
    retVal += (calc << 4U) + calc2;

    return retVal;
}
