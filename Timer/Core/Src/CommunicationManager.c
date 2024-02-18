/*
 * CommunicationManager.c
 *
 *  Created on: Jan 10, 2021
 *      Author: cdromke
 */

#include <string.h>
#include "Configuration.h"
#include "TimeMgmt.h"
#include "LapTimer.h"
#include "MGBTCommProto.h"
#include "CommunicationManager.h"

static MGBTCommandData pendingResponse = {0};
static uint8_t lastResponseSent = 0U;
static uint32_t lastTimeTimeUpdate = 0U;
static uint32_t latestTimestamp = 0U;
static CommTimeType latestTimestampType = NoTimeType;
static uint8_t timeUpdated = 0U;

static uint32_t displayTime = 0U;
static uint8_t newConfig = 0U;

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

static void MoveAllLapsToResponsData(void)
{
    uint8_t currentLapIndex = 0U;
    uint8_t currentDataIndex = 0U;

    for(currentLapIndex = 0U; currentLapIndex < MAXLAPCOUNT; currentLapIndex++)
    {
        if(currentDataIndex < (COMMANDDATAMAXSIZE - 3U))
        {
            uint32_t lapTimeMs = GetLapDurationMs(&laps[currentLapIndex]);
            memcpy(&pendingResponse.data[currentDataIndex], &lapTimeMs, 3U);
            currentDataIndex += 3U;
            pendingResponse.dataLength = currentDataIndex;
        }
    }
}

static void UpdateDisplayedTimeValue(uint32_t* data)
{
    displayTime = (*data);
}

static void PrepareIDData(void)
{
    uint32_t bcdConfig = GetConfigBCDDisplay();
    pendingResponse.data[0] = (uint8_t)(DeviceTypeTimer | DeviceTypeDisplay);
    pendingResponse.dataLength = 1U;
    memcpy(&pendingResponse.data[1], &bcdConfig, sizeof(uint32_t));
    pendingResponse.dataLength += sizeof(uint32_t);
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
            if((operationMode == LaptimerOperation) ||
               (operationMode == SingleRunTimerOperation))
            {
                MoveAllLapsToResponsData();
                pendingResponse.status = 0U;
            }
            else
            {
                pendingResponse.status = 0xFFFFU;
            }
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
            PrepareIDData();
            break;
        }
        case UpdateDisplayedTime:
        {
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            lastResponseSent = 1U;
            if(command->dataLength >= sizeof(uint32_t))
            {
                UpdateDisplayedTimeValue((uint32_t*)command->data);
                pendingResponse.status = 0U;
            }
            else
            {
                pendingResponse.status = 0xFFFFU;
            }
            break;
        }
        case UpdateOpMode:
        {
            memcpy(&pendingResponse, command, GetCommandDataSize(command));
            lastResponseSent = 1U;
            newConfig = command->data[0];
            break;
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

uint8_t CommMgrHasNewDisplayUpdate(void)
{
    uint8_t retVal = 0U;
    if(displayTime > 0U)
    {
        retVal = 1U;
    }
    return retVal;
}

uint32_t CommMgrGetNewDisplayValue(void)
{
    uint32_t retVal = displayTime;
    displayTime = 0U;
    return retVal;
}

uint8_t CommMgrGetNewConfig(void)
{
    uint8_t retVal = 0U;
    if(newConfig != 0U)
    {
        retVal = newConfig;
        newConfig = 0U;
    }
    return retVal;
}
