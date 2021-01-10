/*
 * ConnectedTimestampCollector.c
 *
 *  Created on: Jan 10, 2021
 *      Author: cdromke
 */

#include <string.h>

#include "TimeMgmt.h"
#include "CommunicationManager.h"
#include "ConnectedTimestampCollector.h"

typedef struct
{
    uint32_t time;
    CommTimeType type;
} CommunicatedTimeStamp;

static CommunicatedTimeStamp lastStartTime = {0};
static CommunicatedTimeStamp lastFinishTime = {0};

static void SendTimeValue(CommunicatedTimeStamp* timeStamp)
{
    if(timeStamp != (CommunicatedTimeStamp*)0)
    {
        CommMgrSendTimeValue(timeStamp->type, timeStamp->time);
        memset(timeStamp, 0, sizeof(CommunicatedTimeStamp));
    }
}

void RunConnectedTimestampCollector()
{
    SensorTimestamp timeStamp;

    if(sensorStartStopInterrupt == 1U)
    {
        sensorStartStopInterrupt = 0U;
        GetStartStopSensorTimeStamp(&timeStamp);
        lastStartTime.time = GetMillisecondsFromTimeStampPPS(&timeStamp) * 100U;
        lastStartTime.type = StartSensorTimeStamp;
    }

    if(sensorStopInterrupt == 1U)
    {
        sensorStopInterrupt = 0U;
        GetStopSensorTimeStamp(&timeStamp);
        lastFinishTime.time = GetMillisecondsFromTimeStampPPS(&timeStamp) * 100U;
        lastFinishTime.type = FinishSensorTimeStamp;
    }

    if(CommMgrIsReadyToSendNextTime() == 1U)
    {
        if(lastStartTime.type == StartSensorTimeStamp)
        {
            SendTimeValue(&lastStartTime);

        }
        else if(lastFinishTime.type == FinishSensorTimeStamp)
        {
            SendTimeValue(&lastFinishTime);
        }
    }


}
