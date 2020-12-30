/*
 * Configuration.h
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#ifndef INC_CONFIGURATION_H_
#define INC_CONFIGURATION_H_

#include <stdint.h>
#include "Inputs.h"

#define DISPLAYBRIGHTNESS 0x0F // Range from 0x01 to 0x0F where 0x0F is max brightness
#define LAPTIMERDISPLAYDURATION 20000U

typedef enum
{
    NoOperation = 0U,
    LaptimerOperation = 1U,
    ConnectedTimestampCollector = 2U,
    SingleRunTimerOperation = 3U
} OperationModes;

typedef enum
{
    NoSensors = 0U,
    SingleSensor = 1U,
    DualSensor = 2U
} SensorModes;

typedef enum
{
    RTCInit_SendStartCondition = 0U,
    RTCInit_SendSlaveAddress = 1U,
    RTCInit_SlaveAddressAck = 2U,
    RTCInit_WriteDataToSlave = 3U,
    RTCInit_SendStopCondition = 4U,
    RTCInit_RTCConfigDone = 5U,
    RTCInit_RTCConfigFailed = 6U
} RTCInitStates;

extern OperationModes operationMode;
extern SensorModes sensorMode;
extern uint8_t autoConfigurationDone;
extern uint8_t displayLines;

void RunAutoConfiguration(void);
uint8_t RTCInitSuccesful(void);
uint32_t GetConfigBCDDisplay(void);
#endif /* INC_CONFIGURATION_H_ */
