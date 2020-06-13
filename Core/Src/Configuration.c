/*
 * Configuration.c
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#include "Configuration.h"
#include "TimeMgmt.h"

#include "stm32f1xx_ll_i2c.h"
#include "stm32f1xx_ll_gpio.h"
#include <stdint.h>

#define RTC_SLAVE_ADDRESS 0x68

OperationModes operationMode = NoOperation;
SensorModes sensorMode = NoSensors;
uint8_t autoConfigurationDone = 0U;
uint8_t displayLines = 2U;

static RTCInitStates RTCInitState = RTCInit_SendStartCondition;

static uint8_t slaveDataIndex = 0U;

//Data for DS3231 RTC setup
static const uint8_t rtcConfigBytes[9] = {
		0x0E, //Set pointer to register 0x0E
		0x00, //Set bits RS0 and RS1 to 0 for 1Hz pulse
		};

static void InitRTC(void)
{
	if (LL_I2C_IsActiveFlag_AF(I2C1) || LL_I2C_IsActiveFlag_BERR(I2C1) ||LL_I2C_IsActiveFlag_ARLO(I2C1) ||
			LL_I2C_IsActiveFlag_OVR(I2C1))
	{
		RTCInitState = RTCInit_RTCConfigFailed;
	}

	switch (RTCInitState)
	{
		case RTCInit_SendStartCondition:
		{
			if (LL_I2C_IsActiveFlag_BUSY(I2C1) == 0U)
			{
				LL_I2C_GenerateStartCondition(I2C1);
				RTCInitState = RTCInit_SendSlaveAddress;
			}
			break;
		}
		case RTCInit_SendSlaveAddress:
		{
			if (LL_I2C_IsActiveFlag_SB(I2C1) == 1U)
			{
				LL_I2C_TransmitData8(I2C1, (RTC_SLAVE_ADDRESS << 1));
				RTCInitState = RTCInit_SlaveAddressAck;
			}
			break;
		}
		case RTCInit_SlaveAddressAck:
		{
			if (LL_I2C_IsActiveFlag_ADDR(I2C1) == 1U)
			{
				LL_I2C_ClearFlag_ADDR(I2C1);
				RTCInitState = RTCInit_WriteDataToSlave;
			}
			break;
		}
		case RTCInit_WriteDataToSlave:
		{
			if (LL_I2C_IsActiveFlag_TXE(I2C1) == 1U)
			{
				LL_I2C_TransmitData8(I2C1, rtcConfigBytes[slaveDataIndex]);
				slaveDataIndex++;
				if (slaveDataIndex >= sizeof(rtcConfigBytes))
				{
					RTCInitState = RTCInit_SendStopCondition;
				}
			}
			break;
		}
		case RTCInit_SendStopCondition:
		{
			if ((LL_I2C_IsActiveFlag_BTF(I2C1) == 1U) && (LL_I2C_IsActiveFlag_TXE(I2C1) == 1U))
			{
				LL_I2C_GenerateStopCondition(I2C1);
				RTCInitState = RTCInit_RTCConfigDone;
			}
			break;
		}
		default:
		{
			//State machine done or config failed
			break;
		}
	}
}

uint8_t RTCInitSuccesful(void)
{
	uint8_t retVal = 0U;

	if (RTCInitState == RTCInit_RTCConfigDone)
	{
		retVal = 1U;
	}

	return retVal;
}

void RunAutoConfiguration(void)
{
	InitRTC();


	//For now, this isn't anything exciting. Just finish auto configuration after 2000ms
	if ((GetMillisecondsFromTimeStamp(&systemTime) > 5000U)
			|| (RTCInitSuccesful() == 1U))
	{
		if (LL_GPIO_IsInputPinSet(GPIOB, LL_GPIO_PIN_5) == 1U)
		{
			sensorMode = DualSensor;
			operationMode = SingleRunTimerOperation;
		}
		else
		{
			sensorMode = SingleSensor;
			if (LL_GPIO_IsInputPinSet(GPIOB, LL_GPIO_PIN_4) == 1U)
			{
				operationMode = SingleRunTimerOperation;
			}
			else
			{
				operationMode = LaptimerOperation;
			}
		}
		//Wait for at least 1000ms after startup, to allow power supply to stabilize.
		if (GetMillisecondsFromTimeStamp(&systemTime) > 1000U)
		{
			autoConfigurationDone = 1U;
		}
	}
}

uint32_t GetConfigBCDDisplay(void)
{
	uint32_t retVal = 0U;

	retVal = sensorMode;
	retVal <<= 4;
	retVal |= operationMode;
	retVal <<= 4;
	retVal |= RTCInitState;
	retVal <<= 4;
	retVal |= displayLines;
	retVal <<= 4;
	retVal |= autoConfigurationDone;

	return retVal;


}
