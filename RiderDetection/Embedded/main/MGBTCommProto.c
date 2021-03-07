/*
 * MGBTCommProto.c
 *
 *  Created on: 21 Nov 2020
 *      Author: cdromke
 */


#include <stdint.h>
#include <string.h>
#include "platformconfig.h"

#ifdef CONFIG_IDF_TARGET_ESP32
#include "MGBTTimeMgmt.h"
#else
#include "TimeMgmt.h"
#endif

#ifdef CONFIG_IDF_TARGET_ESP32
#include <driver/uart.h>
#include "driver/gpio.h"
#include "esp_log.h"
#else
#include "UARTBuffer.h"

#endif

#include "MGBTCommProto.h"

#define MAXWAITSTATETIME 1000U
#define MAXSENDINGTIME 10000U
#define RECEIVETIMEOUT 200U

static uint8_t rxDataBuffer[DATABUFFERLENGTH] = {0U};
static uint16_t rxDataBufferPosition = 0U;
static MGBTCommProtoState state = CommProtoIdle;
static uint32_t stateEntryTime = 0U;
static MGBTCommandData rxCommand = {0};
static MGBTCommandData txResponse = {0};
static uint8_t commandIsNew = 0U;
static uint8_t lastResponseSending = 0U;

#ifdef CONFIG_IDF_TARGET_ESP32

static const char* AppName = "MGBTCommProto";
static const uart_config_t uart_config =
{
    .baud_rate = 115200,
    .data_bits = UART_DATA_8_BITS,
    .parity = UART_PARITY_DISABLE,
    .stop_bits = UART_STOP_BITS_1,
    .flow_ctrl = UART_HW_FLOWCTRL_DISABLE,
    .source_clk = UART_SCLK_APB,
};

#endif

#ifndef CONFIG_IDF_TARGET_ESP32
static uint32_t GetTimestampMs(void)
{
    return GetSystemTimeStampMs();
}

#endif

static uint8_t GetDoneSending(void)
{
    uint8_t retVal = 0U;
#ifdef CONFIG_IDF_TARGET_ESP32
    retVal = 1U;
#else
    retVal = UARTBufferTxBufferEmpty(UARTBufferGetUART(MGBT_UART));
#endif

    return retVal;
}

static uint32_t GetTimeInState(void)
{
    return GetTimestampMs() - stateEntryTime;
}

static void ResetData(void)
{
    memset(rxDataBuffer, 0U, sizeof(rxDataBuffer));
    rxDataBufferPosition = 0U;
    memset(&rxCommand, 0U, sizeof(MGBTCommandData));
    memset(&txResponse, 0U, sizeof(MGBTCommandData));
}

void InitCommProto(void)
{
#ifdef CONFIG_IDF_TARGET_ESP32
    // We won't use a buffer for sending data.
    uart_driver_install(MGBT_UART, DATABUFFERLENGTH * 2, 0, 0, NULL, 0);
    uart_param_config(MGBT_UART, &uart_config);
    uart_set_pin(MGBT_UART, TXD_PIN, RXD_PIN, UART_PIN_NO_CHANGE, UART_PIN_NO_CHANGE);
    ESP_LOGI(AppName, "CommandDataSize: %d", sizeof(MGBTCommandData));
#endif
    stateEntryTime = GetTimestampMs();
}

//Comes from serial_reader_l3.c
//ThingMagic-mutated CRC used for messages.
//Notably, not a CCITT CRC-16, though it looks close.
static uint16_t crctable[] =
{
    0x0000,
    0x1021,
    0x2042,
    0x3063,
    0x4084,
    0x50a5,
    0x60c6,
    0x70e7,
    0x8108,
    0x9129,
    0xa14a,
    0xb16b,
    0xc18c,
    0xd1ad,
    0xe1ce,
    0xf1ef,
};

//Calculates the magical CRC value
static uint16_t calculateCRC(uint8_t* u8Buf, uint8_t len)
{
    uint16_t crc = 0xFFFF;

    for(uint8_t i = 0; i < len; i++)
    {
        crc = ((crc << 4) | (u8Buf[i] >> 4)) ^ crctable[crc >> 12];
        crc = ((crc << 4) | (u8Buf[i] & 0x0F)) ^ crctable[crc >> 12];
    }

    return crc;
}

uint16_t GetCommandMaxDataLength(void)
{
    return COMMANDDATAMAXSIZE;
}

uint16_t GetCommandDataSize(MGBTCommandData* data)
{
    uint16_t retVal = 0U;
    if(data != (MGBTCommandData*)0)
    {
        retVal = data->dataLength + 8U;
    }

    return retVal;
}

uint8_t CanSendResponse(void)
{
    uint8_t retVal = 0U;
    if((GetDoneSending() == 1U) &&
       (state != CommProtoReceiving))
    {
        retVal = 1U;
    }

    return retVal;
}

void SendResponse(MGBTCommandData* data, uint8_t lastResponse)
{
    data->crc = calculateCRC((uint8_t*)&data->status, (data->dataLength + 4));
#ifdef CONFIG_IDF_TARGET_ESP32
    uart_write_bytes(MGBT_UART, (char*)data, GetCommandDataSize(data));
#else
    UARTBufferSendData(UARTBufferGetUART(MGBT_UART), (uint8_t*)data, (data->dataLength + 8));
#endif

    lastResponseSending = lastResponse;
    state = CommProtoSending;


}


uint8_t CommandAvailable(void)
{
    uint8_t retVal = 0U;
    if(rxDataBufferPosition > 0U)
    {
        retVal = commandIsNew;
    }
    return retVal;
}

MGBTCommandData* GetAndClearCommand(void)
{
    commandIsNew = 0U;
    return &rxCommand;
}

static uint8_t CheckAllDataArrived(void)
{
    uint8_t retVal = 0U;

    if(rxDataBufferPosition > 1U)
    {
        if(rxDataBuffer[0] == 0xFF)
        {
            if(rxDataBufferPosition >= (rxDataBuffer[1] + 5))
            {
                retVal = 1U;
            }
        }
    }

    return retVal;
}

static void LogRxData(void)
{
#ifdef CONFIG_IDF_TARGET_ESP32
    ESP_LOGI(AppName, "Received data:");
    ESP_LOG_BUFFER_HEXDUMP(AppName, rxDataBuffer, rxDataBufferPosition, ESP_LOG_INFO);
#endif
}

static uint8_t ProcessCommand(void)
{
    uint8_t retVal = 0U;
    memcpy((void*)&rxCommand, &rxDataBuffer[1], rxDataBufferPosition);
    uint16_t calcCrc = calculateCRC(&rxDataBuffer[5], rxCommand.dataLength + 4);
    if(calcCrc == rxCommand.crc)
    {
        retVal = 1U;
    }
    else
    {
#ifdef CONFIG_IDF_TARGET_ESP32
        ESP_LOGE(AppName, "Received CRC 0x%.4X does not match calculated CRC 0x%.4X", rxCommand.crc, calcCrc);
#endif
    }

    return retVal;
}

static uint16_t GetBufferRemaining(void)
{
    return (uint16_t)((DATABUFFERLENGTH - rxDataBufferPosition) - 1);
}

static uint8_t* GetBuffer(void)
{
    return &rxDataBuffer[rxDataBufferPosition];
}

static uint8_t ReceiveData(void)
{
    uint8_t retVal = 0U;
#ifdef CONFIG_IDF_TARGET_ESP32
    int32_t uartResult = uart_read_bytes(MGBT_UART, GetBuffer(), GetBufferRemaining(), 1);
#else
    UARTBuffer* buff = UARTBufferGetUART(MGBT_UART);

    int32_t uartResult = (int32_t)UARTBufferHasNewData(buff);
    if(uartResult > 0)
    {
        UARTBufferGetData(buff, GetBuffer(), GetBufferRemaining());
    }
#endif

    if(uartResult > 0)
    {
        rxDataBufferPosition += (uint8_t)uartResult;
#ifdef CONFIG_IDF_TARGET_ESP32
        ESP_LOGI(AppName, "Read %d bytes, buffer at pos %d", uartResult, rxDataBufferPosition);
#endif
        LogRxData();
        retVal = 1U;
    }

    return retVal;
}

static void CheckReceivedData(void)
{
    if(CheckAllDataArrived() > 0U)
    {
        if(ProcessCommand() > 0U)
        {
            commandIsNew = 1U;
            state = CommProtoWaiting;
        }
    }
}

static void RunProtoIdleState(void)
{
    if(ReceiveData() > 0U)
    {
        state = CommProtoReceiving;
        CheckReceivedData();
    }
}

static void RunProtoReceivingState(void)
{
    ReceiveData();

    CheckReceivedData();

    if(state == CommProtoReceiving)
    {
        if(GetTimeInState() > RECEIVETIMEOUT)
        {
#ifdef CONFIG_IDF_TARGET_ESP32
            ESP_LOGW(AppName, "Timeout on receive state");
#endif
            state = CommProtoIdle;
        }
    }
}

static void RunProtoWaitingState(void)
{
#ifdef CONFIG_IDF_TARGET_ESP32
    uart_flush(MGBT_UART);
#else
    UARTBufferClear(UARTBufferGetUART(MGBT_UART));
#endif
    if(GetTimeInState() > MAXWAITSTATETIME)
    {
#ifdef CONFIG_IDF_TARGET_ESP32
        ESP_LOGW(AppName, "Timeout on wait state");
#endif
        state = CommProtoIdle;
    }
    else
    {
        if(GetTimeInState() < 50)
        {
#ifdef CONFIG_IDF_TARGET_ESP32
            ESP_LOGI(AppName, "Command data length %d of type %d", rxCommand.dataLength, rxCommand.cmdType);
            ESP_LOG_BUFFER_HEXDUMP(AppName, rxCommand.data, rxCommand.dataLength, ESP_LOG_INFO);
#endif
        }
    }
}

static void RunProtoSendingState(void)
{
#ifdef CONFIG_IDF_TARGET_ESP32
    uart_flush(MGBT_UART);
#else
    UARTBufferClear(UARTBufferGetUART(MGBT_UART));
#endif

    if((GetDoneSending() == 1U) &&
       (lastResponseSending == 1U))
    {
        state = CommProtoIdle;
        ResetData();
    }

    if(GetTimeInState() > MAXSENDINGTIME)
    {
#ifdef CONFIG_IDF_TARGET_ESP32
        ESP_LOGW(AppName, "Timeout on sending state");
#endif
        state = CommProtoIdle;
    }
}

void RunCommProto(void)
{
    MGBTCommProtoState oldState = state;
    switch(state)
    {
        case CommProtoIdle:
        {
            RunProtoIdleState();
            break;
        }
        case CommProtoReceiving:
        {
            RunProtoReceivingState();
            break;
        }
        case CommProtoWaiting:
        {
            RunProtoWaitingState();
            break;
        }
        case CommProtoSending:
        {
            RunProtoSendingState();
            break;
        }
        default:
        {
            state = CommProtoIdle;
        }
    }

    if(oldState != state)
    {
        if(state == CommProtoIdle)
        {
            ResetData();
        }
        stateEntryTime = GetTimestampMs();
    }

}

void ClearPacketData(MGBTCommandData* data)
{
    if(data != (MGBTCommandData*)0)
    {
        memset(data->data, 0U, COMMANDDATAMAXSIZE);
    }
}

