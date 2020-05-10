/*
 * RaceTiming.c
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#include "RaceTiming.h"
#include "LapTimer.h"
#include "Display.h"

void RunRaceTiming(void)
{
	RunLapTimer();

	if (lapFinished == 1U)
	{
		lapFinished = 0U;
		UpdateDisplay((previousLap->endTimeStamp - previousLap->startTimeStamp), 5000U);
		ResetRunningDisplayTime();
	}

	RunDisplay();
}
