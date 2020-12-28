/*
 * Inputs.h
 *
 *  Created on: Jun 13, 2020
 *      Author: cdromke
 */

#ifndef INC_INPUTS_H_
#define INC_INPUTS_H_

#include "stm32f1xx_ll_gpio.h"
#include <stdint.h>

#define DEBOUNCETIME 1000U //in increments of 100us, div by 10 for time in ms

typedef struct
{
    GPIO_TypeDef* ioPort;
    uint32_t gpioPin;
} IOPinPort;

typedef struct
{
    IOPinPort ioPin;
    uint32_t timestampLastEdge;
    uint8_t currentState;
    uint8_t currentStateAfterDebounce;
    uint8_t previousStateAfterDebounce;
} DigitalInput;

typedef enum
{
    Button1 = 0U,
    Button2 = 1U,
    JmpSensorCount = 2U,
    JmpOpMode = 3U,
    JmpCommMode = 4U,
    NbOfInputs = 5U
} ButtonsAndJumpers;

extern DigitalInput UserInputs[NbOfInputs];

void InitInputs(void);

void UpdateAllInputs(void);
uint8_t InputGetRisingEdge(DigitalInput* input);
uint8_t InputGetState(DigitalInput* input);

#endif /* INC_INPUTS_H_ */
