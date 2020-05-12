/*
 * Display.c
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#include "Display.h"
#include "TimeMgmt.h"
#include "stm32f1xx_ll_usart.h"

static uint8_t CalculateMinutesComponent(uint32_t milliSeconds);
static uint8_t CalculateSecondsComponent(uint32_t milliSeconds);
static uint16_t CalculateMillisecondsComponent(uint32_t milliSeconds);
static void UpdateDisplayedTime(uint32_t milliseconds);

static uint32_t displayResultUntil = 0U;
static uint8_t permanentResultDisplay = 0U;
static uint32_t displayedResult = 0U;
static uint32_t runningTimeStartTime = 0U;
static uint32_t lastDisplayUpdate = 0U;

static uint8_t uartData[4] = {0};
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
		if (uartTxIndex < sizeof(uartData))
		{
			LL_USART_TransmitData8(USART1, uartData[uartTxIndex]);
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
}

//Abusing the UART to display the time for the moment, by lack of display hardware.
static void UpdateDisplayedTime(uint32_t milliseconds)
{
	if ((uartTxIndex >= sizeof(uartData)) || (uartTxIndex == 0))
	{
		uartData[0] = (uint8_t)CalculateMinutesComponent(milliseconds);
		uartData[1] = (uint8_t)CalculateSecondsComponent(milliseconds);
		uint16_t millisComponent = CalculateMillisecondsComponent(milliseconds);
		uartData[2] = (uint8_t)(millisComponent >> 8);
		uartData[3] = (uint8_t)millisComponent;
		uartTxIndex = 0U;
	}
}

static uint8_t CalculateMinutesComponent(uint32_t milliSeconds)
{
	return milliSeconds / 60000U;
}

static uint8_t CalculateSecondsComponent(uint32_t milliSeconds)
{
	return (milliSeconds % 60000U) / 1000U;
}

static uint16_t CalculateMillisecondsComponent(uint32_t milliSeconds)
{
	return ((milliSeconds % 60000U) % 1000U);
}
