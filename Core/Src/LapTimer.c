/*
 * LapTimer.c
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#ifndef SRC_LAPTIMER_C_
#define SRC_LAPTIMER_C_



#endif /* SRC_LAPTIMER_C_ */

#include "LapTimer.h"
#include "Configuration.h"

static void SingleSensorLaptimer(void);

void RunLapTimer(void)
{
	if (sensorMode == SingleSensor)
	{
		SingleSensorLaptimer();
	}
}

uint8_t IsLastLap(Lap* lap)
{
	uint8_t retVal = 0U;
	if (lap == (&laps[MAXLAPCOUNT-1]))
	{
		retVal = 1U;
	}

	return retVal;
}

void SingleSensorLaptimer(void)
{
	static uint8_t initialLapDone = 0U;

}
