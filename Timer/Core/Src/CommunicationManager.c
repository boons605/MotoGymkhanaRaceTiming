/*
 * CommunicationManager.c
 *
 *  Created on: Jan 10, 2021
 *      Author: cdromke
 */

#include <string.h>

#include "TimeMgmt.h"
#include "MGBTCommProto.h"
#include "CommunicationManager.h"

static MGBTCommandData pendingResponse = {0};
static uint8_t lastResponseSent = 0U;
static uint32_t lastTimeTimeUpdate = 0U;
static uint32_t latestTimestamp = 0U;
static CommTimeType latestTimestampType = NoTimeType;
static uint8_t timeUpdated = 0U;

static void PutTimeInPendingResponse(CommTimeType timeType, uint32_t timeValue)
{
    memcpy(pendingResponse.data, &timeValue, sizeof(timeValue));
    pendingResponse.data[sizeof(timeValue)] = (uint8_t)timeType;
    pendingResponse.dataLength = 1 + sizeof(timeValue);
}

static void SendLatestTimestamp(void)
{
    pendingResponse.cmdType = GetLatestTimeStamp;
    pendingResponse.status = 0U;
    PutTimeInPendingResponse(latestTimestampType, latestTimestamp);
}

static void SendCurrentTime(void)
{
    pendingResponse.cmdType = GetCurrentTime;
    pendingResponse.status = 0U;
    PutTimeInPendingResponse(NoTimeType, GetSystemTimeStampMs());
}

static void ProcessCommand(MGBTCommandData* command)
{
    lastResponseSent = 0U;
    switch(command->cmdType)
    {
        case GetLatestTimeStamp:
        {
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            lastResponseSent = 1U;
            SendLatestTimestamp();
            break;
        }
        case GetAllLaps:
        {
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            lastResponseSent = 1U;
            break;
        }
        case GetCurrentTime:
        {
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            lastResponseSent = 1U;
            SendCurrentTime();
            break;
        }
        case GetIdentification:
        {
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            lastResponseSent = 1U;
            break;
        }
        case UpdateDisplayedTime:
        {
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            lastResponseSent = 1U;
        }
        default:
        {
            break;
        }
    }
}

static void ProcessManagerWork(void)
{
    uint32_t sysTimeStamp = GetSystemTimeStampMs();
    if(pendingResponse.cmdType == NoOperation)
    {
        if(timeUpdated != 0U)
        {
            timeUpdated = 0U;
            SendLatestTimestamp();
            lastResponseSent = 1U;
        }
        else if((lastTimeTimeUpdate + TIMEUPDATEPERIOD) <= sysTimeStamp)
        {
            lastTimeTimeUpdate = sysTimeStamp;
            SendCurrentTime();
            lastResponseSent = 1U;
        }
    }
}

static void SendPendingResponse(void)
{
    if(pendingResponse.cmdType != NoOperation)
    {
        SendResponse(&pendingResponse, lastResponseSent);
        if(lastResponseSent == 1U)
        {
            memset(&pendingResponse, 0, sizeof(MGBTCommandData));
        }
    }
}

void RunCommunicationManager(void)
{
    RunCommProto();
    if(CommandAvailable() != 0U)
    {
        ProcessCommand(GetAndClearCommand());
    }
    ProcessManagerWork();
    if(CanSendResponse() != 0U)
    {
        SendPendingResponse();
    }
}

void CommMgrSendTimeValue(CommTimeType timeType, uint32_t timeValue)
{
    timeUpdated = 1U;
    latestTimestampType = timeType;
    latestTimestamp = timeValue;
}

uint8_t CommMgrIsReadyToSendNextTime(void)
{
    uint8_t retVal = 0U;
    if(timeUpdated == 0U)
    {
        retVal = 1U;
    }
    return retVal;
}
