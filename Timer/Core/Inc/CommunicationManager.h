/*
 * CommunicationManager.h
 *
 *  Created on: Jan 10, 2021
 *      Author: cdromke
 */

#ifndef INC_COMMUNICATIONMANAGER_H_
#define INC_COMMUNICATIONMANAGER_H_

#define TIMEUPDATEPERIOD 1000U

typedef enum
{
    NoTimeType = 0,
    StartSensorTimeStamp = 1,
    FinishSensorTimeStamp = 2,
    LastLapTime = 3,
    CurrentLapDisplayTime = 4
} CommTimeType;


typedef enum
{
    Idle = 0U,
} CommManagerState;

void RunCommunicationManager(void);
void CommMgrSendTimeValue(CommTimeType timeType, uint32_t timeValue);
uint8_t CommMgrIsReadyToSendNextTime(void);
uint8_t CommMgrHasNewDisplayUpdate(void);
uint32_t CommMgrGetNewDisplayValue(void);
uint8_t CommMgrGetNewConfig(void);

#endif /* INC_COMMUNICATIONMANAGER_H_ */
