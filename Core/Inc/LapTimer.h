/*
 * LapTimer.h
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#ifndef INC_LAPTIMER_H_
#define INC_LAPTIMER_H_

#define MAXLAPCOUNT 32

typedef struct
{
	uint32_t startTimeStamp;
	uint32_t endTimeStamp;
} Lap;

extern Lap laps[MAXLAPCOUNT];
extern Lap* currentLap;

void RunLapTimer(void);

uint8_t IsLastLap(Lap* lap);

#endif /* INC_LAPTIMER_H_ */
