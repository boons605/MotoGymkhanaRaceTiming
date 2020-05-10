#ifndef TIMEMGMT_H
#define TIMEMGMT_H
#include <stdint.h>
#define MIN_SENSOR_INTERRUPT_WAIT 2000U

typedef struct {
	uint32_t timeStampPps;
	uint32_t ppsOffsetMs;
	uint32_t timeStampMs;
} SensorTimestamp;

extern volatile SensorTimestamp systemTime;
extern volatile SensorTimestamp sensorStartStopTimeStamp;
extern volatile SensorTimestamp sensorStopTimeStamp;

extern volatile uint8_t sensorStartStopInterrupt;
extern volatile uint8_t sensorStopInterrupt;
extern volatile uint8_t ppsTick;


uint32_t CalculateTotalMillisecondsBetweenTimeStamps(SensorTimestamp* startTime, SensorTimestamp* endTime);
uint32_t CalculateMinutesComponent(uint32_t milliSeconds);
uint32_t CalculateSecondsComponent(uint32_t milliSeconds);
uint32_t CalculateMillisecondsComponent(uint32_t milliSeconds);


#endif
