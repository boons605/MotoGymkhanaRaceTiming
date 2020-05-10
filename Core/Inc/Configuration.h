/*
 * Configuration.h
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#ifndef INC_CONFIGURATION_H_
#define INC_CONFIGURATION_H_

typedef enum
{
	NoOperation = 0U,
	LaptimerOperation = 1U,
	ConnectedTimestampCollector = 2U
} OperationModes;

typedef enum
{
	NoSensors = 0U,
	SingleSensor = 1U,
	DualSensor = 2U
} SensorModes;

extern OperationModes operationMode;
extern SensorModes sensorMode;
extern uint8_t autoConfigurationDone;

void RunAutoConfiguration(void);
#endif /* INC_CONFIGURATION_H_ */
