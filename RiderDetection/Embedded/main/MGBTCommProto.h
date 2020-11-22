/*
 * MGBTCommProto.h
 *
 *  Created on: 21 Nov 2020
 *      Author: cdromke
 */

#ifndef MAIN_MGBTCOMMPROTO_H_
#define MAIN_MGBTCOMMPROTO_H_

#include <stdint.h>

#define COMMANDDATAMAXSIZE 128U
#define DATABUFFERLENGTH 255U

#define MGBT_UART UART_NUM_1
#define TXD_PIN (GPIO_NUM_4)
#define RXD_PIN (GPIO_NUM_5)

typedef enum {
	CommProtoIdle = 0U,
	CommProtoReceiving = 1U,
	CommProtoWaiting = 2U,
	CommProtoSending = 3U
} MGBTCommProtoState;

typedef enum {
	NoOperation = 0U
} MGBTCommandType;

typedef struct {
	uint16_t dataLength;
	uint16_t crc;
	uint16_t status;
	MGBTCommandType cmdType : 16;
	uint8_t data[COMMANDDATAMAXSIZE];
} MGBTCommandData;

uint8_t CommandAvailable(void);
void RunCommProto(void);
void InitCommProto(void);

#endif /* MAIN_MGBTCOMMPROTO_H_ */
