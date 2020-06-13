/*
 * Display.c
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */
#include "Configuration.h"
#include "Display.h"
#include "TimeMgmt.h"
#include "stm32f1xx_ll_usart.h"
#include "Max7219Display.h"
#include "Max7219DLDWDisplay.h"


static uint8_t CalculateMinutesComponent(uint32_t milliSeconds);
static uint8_t CalculateSecondsComponent(uint32_t milliSeconds);
static uint16_t CalculateMillisecondsComponent(uint32_t milliSeconds);
static void UpdateDisplayedTime(uint32_t milliseconds, uint8_t cutOffLastDigits);

static uint32_t displayResultUntil = 0U;
static uint8_t permanentResultDisplay = 0U;
static uint32_t displayedResult = 0U;
static uint32_t runningTimeStartTime = 0U;
static uint32_t lastDisplayUpdate = 0U;

static uint8_t bcdTimeData[4] = {0};
static uint8_t uartTxIndex = 0U;
static uint8_t displayConfig = 1U;




void UpdateDisplay(uint32_t newTimeInMs, uint32_t displayDurationInMs)
{
	if (displayDurationInMs > 0U)
	{
		displayResultUntil = GetMillisecondsFromTimeStamp(&systemTime) + displayDurationInMs;
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

void ResetRunningDisplayTime(void)
{
	runningTimeStartTime = GetMillisecondsFromTimeStamp(&systemTime);
	permanentResultDisplay = 0U;
	displayConfig = 0U;
}

static void SendUartBuffer(void)
{
	if (LL_USART_IsActiveFlag_TXE(USART1))
	{
		if (uartTxIndex < sizeof(bcdTimeData))
		{
			LL_USART_TransmitData8(USART1, bcdTimeData[uartTxIndex]);
			uartTxIndex++;
		}
	}
}


void RunDisplay(void)
{
	uint32_t timeStamp = GetMillisecondsFromTimeStamp(&systemTime);

	if ((timeStamp < displayResultUntil) ||
			(permanentResultDisplay == 1U))
	{
		if (((lastDisplayUpdate+500U) < timeStamp))
		{
			UpdateDisplayedTime(displayedResult, 0U);

		}
	}
	else
	{
		if ((lastDisplayUpdate+100U) < timeStamp)
		{
			UpdateDisplayedTime(timeStamp - runningTimeStartTime, 1U);
		}
	}


	SendUartBuffer();

	if (displayLines == 2U)
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
	if ((uartTxIndex >= sizeof(bcdTimeData)) || (uartTxIndex == 0))
	{
		bcdTimeData[0] = (uint8_t)CalculateMinutesComponent(milliseconds);
		bcdTimeData[1] = (uint8_t)CalculateSecondsComponent(milliseconds);
		uint16_t millisComponent = CalculateMillisecondsComponent(milliseconds);
		bcdTimeData[2] = (uint8_t)(millisComponent >> 8);
		bcdTimeData[3] = (uint8_t)millisComponent;
		uartTxIndex = 0U;
		uint32_t bcdDisplayData = 0U;

		bcdDisplayData |= ((uint32_t)bcdTimeData[0]) << 20;
		bcdDisplayData |= ((uint32_t)bcdTimeData[1]) << 12;
		bcdDisplayData |= ((uint32_t)bcdTimeData[2]) << 8;
		if (cutOffLastDigits == 0U)
		{
			bcdDisplayData |= ((uint32_t)bcdTimeData[3]) << 0;
		}
		else
		{
			bcdDisplayData |= 0xCC;
		}

		if (displayConfig == 1U)
		{
			bcdDisplayData = GetConfigBCDDisplay();
		}

		if (displayLines == 2U)
		{
			UpdateMax7219DLDWDisplay(bcdDisplayData);
		}
		else
		{
			UpdateMax7219Display(bcdDisplayData);
		}
		lastDisplayUpdate = GetMillisecondsFromTimeStamp(&systemTime);
	}


}

static uint8_t CalculateMinutesComponent(uint32_t milliSeconds)
{
	return milliSeconds / 60000U;
}

static uint8_t CalculateSecondsComponent(uint32_t milliSeconds)
{
	uint8_t retVal = (milliSeconds % 60000U) / 1000U;
	uint8_t calc = retVal;
	calc /= 10U;
	retVal -= (calc*10U);
	retVal += (calc << 4U);

	return retVal;
}

static uint16_t CalculateMillisecondsComponent(uint32_t milliSeconds)
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
