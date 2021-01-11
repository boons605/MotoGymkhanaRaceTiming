/*
 * UARTBuffer.c
 *
 *  Created on: Jan 9, 2021
 *      Author: cdromke
 */

#include <string.h>

#include "UARTBuffer.h"
#include "stm32f1xx.h"
#include "stm32f1xx_ll_usart.h"

#define NO_OF_UART 2

static UARTBuffer uarts[NO_OF_UART] = {0};

UARTBuffer* UARTBufferGetUART(uint8_t index)
{
    UARTBuffer* buff = (UARTBuffer*)0;
    if(index < NO_OF_UART)
    {
        buff = &uarts[index];
    }
    return buff;
}

void UARTBufferProcessReceivedByte(UARTBuffer* buffer, uint8_t byte)
{
    if(buffer != (UARTBuffer*)0)
    {
        buffer->rxBuffer[buffer->currentRxBufferPosition] = byte;
        buffer->currentRxBufferPosition++;
        if(buffer->currentRxBufferPosition >= UART_BUFFER_SIZE)
        {
            buffer->currentRxBufferPosition = 0U;
        }
    }
}

uint8_t UARTBufferHasNewData(UARTBuffer* buffer)
{
    uint8_t retVal = 0U;
    if(buffer != (UARTBuffer*)0)
    {
        if(buffer->rxBufferStartPosition != buffer->currentRxBufferPosition)
        {
            if(buffer->rxBufferStartPosition < buffer->currentRxBufferPosition)
            {
                retVal = buffer->currentRxBufferPosition - buffer->rxBufferStartPosition;
            }
            else
            {
                retVal = (UART_BUFFER_SIZE - buffer->rxBufferStartPosition) + buffer->currentRxBufferPosition;
            }
        }
    }

    return retVal;
}

void UARTBufferGetData(UARTBuffer* buffer, uint8_t* dstBuffer, uint8_t length)
{
    if(buffer != (UARTBuffer*)0)
    {
        uint8_t index;
        LL_USART_DisableIT_RXNE(buffer->uartHandle);
        uint8_t availableDataLength = UARTBufferHasNewData(buffer);
        uint8_t returnedDataLength = availableDataLength;
        if(returnedDataLength > length)
        {
            returnedDataLength = length;
        }

        for(index = 0U; index < returnedDataLength; index++)
        {
            if((buffer->rxBufferStartPosition + index) >= UART_BUFFER_SIZE)
            {
                dstBuffer[index] = buffer->rxBuffer[((buffer->rxBufferStartPosition + index) - UART_BUFFER_SIZE)];
            }
            else
            {
                dstBuffer[index] = buffer->rxBuffer[buffer->rxBufferStartPosition + index];
            }
        }


        if(returnedDataLength == availableDataLength)
        {
            buffer->rxBufferStartPosition = buffer->currentRxBufferPosition;
        }
        else
        {
            buffer->rxBufferStartPosition += returnedDataLength;
            if(buffer->rxBufferStartPosition >= UART_BUFFER_SIZE)
            {
                buffer->rxBufferStartPosition -= UART_BUFFER_SIZE;
            }
        }

        LL_USART_EnableIT_RXNE(buffer->uartHandle);
    }
}

void UARTBufferClear(UARTBuffer* buffer)
{
    if(buffer != (UARTBuffer*)0)
    {
        buffer->rxBufferStartPosition = buffer->currentRxBufferPosition;
    }
}

void UARTBufferProcessTXData(UARTBuffer* buffer)
{
    if(buffer != (UARTBuffer*)0)
    {
        if(LL_USART_IsActiveFlag_TXE(buffer->uartHandle))
        {
            if(buffer->txBufferTxPosition != buffer->currentTxBufferPosition)
            {
                LL_USART_TransmitData8(buffer->uartHandle, buffer->txBuffer[buffer->txBufferTxPosition]);
                buffer->txBufferTxPosition++;
                if(buffer->txBufferTxPosition >= sizeof(buffer->txBuffer))
                {
                    buffer->txBufferTxPosition = 0U;
                }
            }
            else
            {
                buffer->currentTxBufferPosition = 0U;
                buffer->txBufferTxPosition = 0U;
            }
        }
    }
}

void UARTBufferRunTxWork(void)
{
    uint8_t index;
    for(index = 0U; index < NO_OF_UART; index++)
    {
        UARTBufferProcessTXData(&uarts[index]);
    }
}

uint8_t UARTBufferTxBufferEmpty(UARTBuffer* buffer)
{
    uint8_t retVal = 0U;
    if(buffer != (UARTBuffer*)0)
    {
        if(buffer->currentTxBufferPosition == buffer->txBufferTxPosition)
        {
            retVal = 1U;
        }
    }

    return retVal;
}

void UARTBufferSendData(UARTBuffer* buffer, uint8_t* data, uint8_t length)
{
    if((buffer != (UARTBuffer*)0) &&
       (data != (uint8_t*)0) &&
       length > 0)
    {
        uint8_t bytesToCopy = length;
        if((buffer->currentTxBufferPosition + bytesToCopy) >= UART_BUFFER_SIZE)
        {
            bytesToCopy = UART_BUFFER_SIZE - buffer->currentTxBufferPosition;
        }
        memcpy(&buffer->txBuffer[buffer->currentTxBufferPosition], data, bytesToCopy);
    }
}
