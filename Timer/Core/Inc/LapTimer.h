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
#define MAXSIMULTANEOUSRIDERS 10

typedef struct
{
    uint32_t startTimeStamp;
    uint32_t endTimeStamp;
} Lap;

extern Lap laps[MAXLAPCOUNT];
extern uint8_t lapFinished;
extern uint8_t newRunStarted;

void RunStandAloneTimer(void);
Lap* GetPreviousLap(void);
uint8_t IsFirstLap(void);
uint32_t GetCurrentLapStartTime(void);
Lap* GetCurrentLap(void);
uint8_t GetLapIndex(Lap* lap);
uint32_t GetLapDurationMs(Lap* lap);
void InvalidateLapIndex(uint8_t index);


#endif /* INC_LAPTIMER_H_ */
