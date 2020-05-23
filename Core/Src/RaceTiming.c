/*
 * RaceTiming.c
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#include "Configuration.h"
#include "RaceTiming.h"
#include "LapTimer.h"
#include "Display.h"

void RunRaceTiming(void)
{
	switch (operationMode)
	{
		case LaptimerOperation:
		{
			RunStandAloneTimer();
			if (lapFinished == 1U)
			{
				lapFinished = 0U;
				UpdateDisplay((previousLap->endTimeStamp - previousLap->startTimeStamp), 5000U);
				ResetRunningDisplayTime();
			}
			break;
		}
		case SingleRunTimerOperation:
		{
			RunStandAloneTimer();
			if (lapFinished == 1U)
			{
				lapFinished = 0U;
				UpdateDisplay((previousLap->endTimeStamp - previousLap->startTimeStamp), 0U);
			}

			if (newRunStarted == 1U)
			{
				newRunStarted = 0U;
				ResetRunningDisplayTime();
			}

			break;
		}
		default:
		{
			//Do nothing
			break;
		}
	}

	RunDisplay();
}
