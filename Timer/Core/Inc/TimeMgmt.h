#ifndef TIMEMGMT_H
#define TIMEMGMT_H
#include <stdint.h>
#define MIN_SENSOR_INTERRUPT_WAIT 20000U

typedef struct
{
    uint32_t timeStampPps;
    uint32_t ppsOffset100us;
    uint32_t timeStamp100us;
} SensorTimestamp;

extern volatile SensorTimestamp systemTime;
extern volatile SensorTimestamp sensorStartStopTimeStamp;
extern volatile SensorTimestamp sensorStopTimeStamp;

extern volatile uint8_t sensorStartStopInterrupt;
extern volatile uint8_t sensorStopInterrupt;
extern volatile uint8_t ppsTick;


uint32_t GetMillisecondsFromTimeStampPPS(SensorTimestamp* timeStamp);
uint32_t GetMillisecondsFromTimeStamp(SensorTimestamp* timeStamp);
uint32_t GetSystemTimeStampMs(void);

void GetStartStopSensorTimeStamp(SensorTimestamp* copy);
void GetStopSensorTimeStamp(SensorTimestamp* copy);

#endif
