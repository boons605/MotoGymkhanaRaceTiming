/*
 * UARTBuffer.h
 *
 *  Created on: Jan 9, 2021
 *      Author: cdromke
 */

#ifndef INC_UARTBUFFER_H_
#define INC_UARTBUFFER_H_

#include "stm32f1xx.h"

#define UART_BUFFER_SIZE 256U

typedef struct
{
    uint8_t rxBuffer[UART_BUFFER_SIZE];
    uint8_t txBuffer[UART_BUFFER_SIZE];
    uint16_t currentRxBufferPosition;
    uint16_t rxBufferStartPosition;
    uint16_t currentTxBufferPosition;
    uint16_t txBufferTxPosition;
    USART_TypeDef* uartHandle;
} UARTBuffer;

void UARTBufferProcessReceivedByte(UARTBuffer* buffer, uint8_t byte);
void UARTBufferClear(UARTBuffer* buffer);
void UARTBufferGetData(UARTBuffer* buffer, uint8_t* dstBuffer, uint8_t length);
uint8_t UARTBufferHasNewData(UARTBuffer* buffer);
UARTBuffer* UARTBufferGetUART(uint8_t index);
void UARTBufferRunTxWork(void);
uint8_t UARTBufferTxBufferEmpty(UARTBuffer* buffer);
void UARTBufferSendData(UARTBuffer* buffer, uint8_t* data, uint8_t length);


#endif /* INC_UARTBUFFER_H_ */
