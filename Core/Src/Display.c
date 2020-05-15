/*
 * Display.c
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#include "Display.h"
#include "TimeMgmt.h"
#include "stm32f1xx_ll_usart.h"
#include "Max7219Display.h"


static uint8_t CalculateMinutesComponent(uint32_t milliSeconds);
static uint8_t CalculateSecondsComponent(uint32_t milliSeconds);
static uint16_t CalculateMillisecondsComponent(uint32_t milliSeconds);
static void UpdateDisplayedTime(uint32_t milliseconds);

static uint32_t displayResultUntil = 0U;
static uint8_t permanentResultDisplay = 0U;
static uint32_t displayedResult = 0U;
static uint32_t runningTimeStartTime = 0U;
static uint32_t lastDisplayUpdate = 0U;

static uint8_t bcdTimeData[4] = {0};
static uint8_t uartTxIndex = 0U;





void UpdateDisplay(uint32_t newTimeInMs, uint32_t displayDurationInMs)
{
	if (displayDurationInMs > 0U)
	{
		displayResultUntil = systemTime.timeStampMs + displayDurationInMs;
	}
	else
	{
		permanentResultDisplay = 1U;
	}

	displayedResult = newTimeInMs;
	//Force display update
	lastDisplayUpdate = 0U;


}

void ResetRunningDisplayTime(void)
{
	runningTimeStartTime = systemTime.timeStampMs;
	permanentResultDisplay = 0U;
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
	uint32_t timeStamp = systemTime.timeStampMs;

	if ((timeStamp < displayResultUntil) ||
			(permanentResultDisplay == 1U))
	{
		if (((lastDisplayUpdate+500U) < timeStamp))
		{
			UpdateDisplayedTime(displayedResult);

		}
	}
	else
	{
		if ((lastDisplayUpdate+100U) < timeStamp)
		{
			UpdateDisplayedTime(timeStamp - runningTimeStartTime);
		}
	}


	SendUartBuffer();
	RunMax7219Display();
}

//Abusing the UART to display the time for the moment, by lack of display hardware.
static void UpdateDisplayedTime(uint32_t milliseconds)
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

		bcdDisplayData |= ((uint32_t)bcdTimeData[0]) << 24;
		bcdDisplayData |= ((uint32_t)bcdTimeData[1]) << 16;
		bcdDisplayData |= ((uint32_t)bcdTimeData[2]) << 8;
		bcdDisplayData |= ((uint32_t)bcdTimeData[3]) << 0;

		UpdateMax7219Display(bcdDisplayData);
		lastDisplayUpdate = systemTime.timeStampMs;
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
