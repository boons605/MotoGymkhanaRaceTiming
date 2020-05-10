/*
 * LapTimer.h
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#ifndef INC_LAPTIMER_H_
#define INC_LAPTIMER_H_

#include <stdint.h>

#define MAXLAPCOUNT 32

typedef struct
{
	uint32_t startTimeStamp;
	uint32_t endTimeStamp;
} Lap;

extern Lap laps[MAXLAPCOUNT];
extern Lap* currentLap;
extern Lap* previousLap;
extern uint8_t lapFinished;

void RunLapTimer(void);


#endif /* INC_LAPTIMER_H_ */
