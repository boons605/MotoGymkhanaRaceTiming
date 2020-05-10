/*
 * Configuration.c
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#include "Configuration.h"
#include "TimeMgmt.h"

OperationModes operationMode = NoOperation;
SensorModes sensorMode = NoSensors;
uint8_t autoConfigurationDone = 0U;

void RunAutoConfiguration(void)
{
	//For now, this isn't anything exciting. Just finish auto configuration after 2000ms
	if (systemTime.timeStampMs > 2000U)
	{
		operationMode = LaptimerOperation;
		sensorMode = SingleSensor;
		autoConfigurationDone = 1U;
	}
}
