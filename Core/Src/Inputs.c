/*
 * Inputs.c
 *
 *  Created on: Jun 13, 2020
 *      Author: cdromke
 */

#include "Inputs.h"
#include "TimeMgmt.h"
#include "stm32f1xx_ll_gpio.h"

#define INPUTUPDATETIME 100U // In increments of 100us. Div by 10 for update cycle time in ms.

DigitalInput UserInputs[NbOfInputs] = { 0 };

static uint32_t NextUpdateTime;

uint8_t InitInputs(void)
{
	UserInputs[Button1].ioPin.ioPort = GPIOA;
	UserInputs[Button1].ioPin.gpioPin = LL_GPIO_PIN_0;

	UserInputs[Button2].ioPin.ioPort = GPIOA;
	UserInputs[Button2].ioPin.gpioPin = LL_GPIO_PIN_1;

	UserInputs[JmpCommMode].ioPin.ioPort = GPIOB;
	UserInputs[JmpCommMode].ioPin.gpioPin = LL_GPIO_PIN_10;

	UserInputs[JmpOpMode].ioPin.ioPort = GPIOB;
	UserInputs[JmpOpMode].ioPin.gpioPin = LL_GPIO_PIN_8;

	UserInputs[JmpSensorCount].ioPin.ioPort = GPIOB;
	UserInputs[JmpSensorCount].ioPin.gpioPin = LL_GPIO_PIN_9;

	NextUpdateTime = systemTime.timeStamp100us + INPUTUPDATETIME;
}

uint8_t UpdateAllInputs(void)
{

	uint32_t currentTimeStamp = systemTime.timeStamp100us;
	if (currentTimeStamp > NextUpdateTime)
	{
		uint8_t index;

		for (index = (uint8_t)Button1; index < (uint8_t)NbOfInputs; index++)
		{
			DigitalInput* input = &UserInputs[index];
			uint8_t inputState = input->currentState;
			input->currentState = (uint8_t)LL_GPIO_IsInputPinSet(input->ioPin.ioPort, input->ioPin.gpioPin);

			if (input->currentState == inputState)
			{
				if ((input->timestampLastEdge + DEBOUNCETIME) < currentTimeStamp)
				{
					input->currentStateAfterDebounce = input->currentState;
				}
			}
			else
			{
				input->timestampLastEdge = currentTimeStamp;
			}

		}

		NextUpdateTime += INPUTUPDATETIME;
	}
}

uint8_t InputGetState(DigitalInput * input)
{
	return input->currentStateAfterDebounce;
}

uint8_t InputGetRisingEdge(DigitalInput* input)
{
	uint8_t retVal = 0U;
	if ((input->currentStateAfterDebounce == 1U) && (input->previousStateAfterDebounce == 0U))
	{
		retVal = 1U;
	}

	input->previousStateAfterDebounce = input->currentStateAfterDebounce;
	return retVal;
}
