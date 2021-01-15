/*
 * MGBTCommProto.h
 *
 *  Created on: 21 Nov 2020
 *      Author: cdromke
 */

#ifndef MAIN_MGBTCOMMPROTO_H_
#define MAIN_MGBTCOMMPROTO_H_

#include <stdint.h>
#include "platformconfig.h"

#define COMMANDDATAMAXSIZE 128U
#define DATABUFFERLENGTH 255U

#ifdef CONFIG_IDF_TARGET_ESP32

#define MGBT_UART UART_NUM_1
#define TXD_PIN (GPIO_NUM_4)
#define RXD_PIN (GPIO_NUM_5)
#else

#define MGBT_UART 0

#endif

typedef enum
{
    CommProtoIdle = 0U,
    CommProtoReceiving = 1U,
    CommProtoWaiting = 2U,
    CommProtoSending = 3U
} MGBTCommProtoState;

typedef enum
{
    NoOperation = 0U,
    AddAllowedDevice = 1U,
    RemoveAllowedDevice = 2U,
    ListAllowedDevices = 3U,
    ListDetectedDevices = 4U,
    GetClosestDevice = 5U,

    GetLatestTimeStamp = 101U,
    GetAllLaps = 102U,
    GetCurrentTime = 103U,
    UpdateDisplayedTime = 104U,
    UpdateOpMode = 105U,
    GetIdentification = 255U

} MGBTCommandType;

typedef struct
{
    uint16_t dataLength;
    uint16_t crc;
    uint16_t status;
    uint16_t cmdType;
    uint8_t data[COMMANDDATAMAXSIZE];
} MGBTCommandData;

typedef enum
{
	DeviceTypeNone = 0U,
	DeviceTypeTimer = 1U,
	DeviceTypeIdentifier = 2U,
	DeviceTypeDisplay = 4U,
	DeviceTypeStartRelease = 8U
} DeviceTypes;

uint8_t CommandAvailable(void);
MGBTCommandData* GetAndClearCommand(void);
uint8_t CanSendResponse(void);
void SendResponse(MGBTCommandData* data, uint8_t lastResponse);
void RunCommProto(void);
void InitCommProto(void);
uint16_t GetCommandDataSize(MGBTCommandData* data);
uint16_t GetCommandMaxDataLength(void);
void ClearPacketData(MGBTCommandData* data);

#endif /* MAIN_MGBTCOMMPROTO_H_ */
