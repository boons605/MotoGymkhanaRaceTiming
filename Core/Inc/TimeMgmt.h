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


uint32_t GetMillisecondsFromTimeStamp(SensorTimestamp* timeStamp);

void GetStartStopSensorTimeStamp(SensorTimestamp* copy);
void GetStopSensorTimeStamp(SensorTimestamp* copy);

#endif
