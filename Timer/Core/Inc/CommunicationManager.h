/*
 * CommunicationManager.h
 *
 *  Created on: Jan 10, 2021
 *      Author: cdromke
 */

#ifndef INC_COMMUNICATIONMANAGER_H_
#define INC_COMMUNICATIONMANAGER_H_

typedef enum
{
    NoTimeType = 0,
    StartSensorTimeStamp = 1,
    FinishSensorTimeStamp = 2,
    LastLapTime = 3
} CommTimeType;

void RunCommunicationManager(void);
void CommMgrSendTimeValue(CommTimeType timeType, uint32_t timeValue);
uint8_t CommMgrIsReadyToSendNextTime(void);

#endif /* INC_COMMUNICATIONMANAGER_H_ */
