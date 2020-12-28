/*
 * Max7219Display.c
 *
 *  Created on: May 14, 2020
 *      Author: r.boonstra
 */

#include "Max7219Display.h"
#include "Configuration.h"
#include "stm32f1xx_ll_spi.h"
#include "stm32f1xx_ll_gpio.h"

#define DISPLAYCOUNT 4

static const uint8_t CH[12][8] =
{
    {0x06, 0x09, 0x09, 0x09, 0x09, 0x09, 0x06, 0x00}, // 0
    {0x02, 0x06, 0x02, 0x02, 0x02, 0x02, 0x07, 0x00}, // 1
    {0x06, 0x09, 0x01, 0x02, 0x04, 0x04, 0x0F, 0x00}, // 2
    {0x06, 0x09, 0x01, 0x06, 0x01, 0x09, 0x06, 0x00}, // 3
    {0x01, 0x03, 0x05, 0x09, 0x0F, 0x01, 0x01, 0x00}, // 4
    {0x0F, 0x08, 0x0E, 0x01, 0x01, 0x09, 0x06, 0x00}, // 5
    {0x06, 0x08, 0x08, 0x06, 0x09, 0x09, 0x06, 0x00}, // 6
    {0x0F, 0x01, 0x01, 0x02, 0x04, 0x08, 0x08, 0x00}, // 7
    {0x06, 0x09, 0x09, 0x06, 0x09, 0x09, 0x06, 0x00}, // 8
    {0x06, 0x09, 0x09, 0x06, 0x01, 0x01, 0x06, 0x00}, // 9
    {0x00, 0x00, 0x02, 0x00, 0x00, 0x02, 0x00, 0x00}, // :
    {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01} // .
};

static const uint16_t digits[] =
{
    REG_DIGIT_0,
    REG_DIGIT_1,
    REG_DIGIT_2,
    REG_DIGIT_3,
    REG_DIGIT_4,
    REG_DIGIT_5,
    REG_DIGIT_6,
    REG_DIGIT_7
};

static const uint16_t max7219InitActions[4] =
{
    REG_SHUTDOWN | 0x01,
    REG_DECODE_MODE | 0x00,
    REG_SCAN_LIMIT | 0x07,
    REG_INTENSITY | DISPLAYBRIGHTNESS
};

static uint16_t max7219SpiBuffer[DISPLAYCOUNT] = {0};
static uint8_t max7219dataIndex = 0U;
static uint8_t max7219dataTransmissionState = 0U;
static uint8_t initState = 0U;
static uint8_t newTime = 0U;
static uint32_t timeDataBCD = 0U;
static uint8_t displayLine = 0U;


static void sendData(uint16_t data, uint8_t index)
{
    max7219SpiBuffer[index] = data;
    if(max7219dataTransmissionState == 0U)
    {
        max7219dataTransmissionState = 1U;
    }
}


static void SendSPIBuffer(void)
{
    LL_GPIO_ResetOutputPin(GPIOB, LL_GPIO_PIN_12);
    switch(max7219dataTransmissionState)
    {
        case 1U:
        {
            if(max7219dataIndex < DISPLAYCOUNT)
            {
                if(LL_SPI_IsActiveFlag_TXE(SPI2))
                {
                    LL_SPI_TransmitData16(SPI2, max7219SpiBuffer[max7219dataIndex]);
                    max7219dataIndex++;
                }
            }
            else
            {
                max7219dataIndex = 0U;
                max7219dataTransmissionState++;
            }
            break;
        }
        case 2U:
        {
            if(LL_SPI_IsActiveFlag_BSY(SPI2) == 0U)
            {
                LL_GPIO_SetOutputPin(GPIOB, LL_GPIO_PIN_12);
                max7219dataTransmissionState = 0U;
                LL_GPIO_ResetOutputPin(GPIOB, LL_GPIO_PIN_12);
            }
            break;
        }
        default:
        {
            //Wait for new data.
            break;
        }
    }
}



void UpdateMax7219Display(uint32_t data)
{
    timeDataBCD = data;
    //timeDataBCD = 0x000789123;
    newTime = 1U;
    displayLine = 0U;
}

void InitMax7219Display(void)
{
    if(max7219dataTransmissionState == 0U)
    {
        uint8_t index = 0U;
        for(index = 0U; index < 4; index++)
        {
            sendData(max7219InitActions[initState], index);
        }
        initState++;
    }
}

static uint32_t GenerateDisplayData(void)
{
    uint8_t index = 0U;
    uint32_t retVal = 0U;
    for(index = 6U; index > 0U; index--)
    {
        uint8_t digit = ((timeDataBCD >> ((index - 1) * 4)) & 0x0F);
        if(digit < 10U)
        {
            if((digit != 0U) || (retVal != 0U))
            {
                retVal |= CH[digit][displayLine];
            }

        }

        if(index == 6U)
        {
            retVal <<= 3U;
            if(retVal != 0U)
            {
                retVal |= CH[10][displayLine];
            }

            retVal <<= 4U;

        }
        else if(index == 4U)
        {
            retVal <<= 2U;
            retVal |= CH[11][displayLine];
            retVal <<= 4U;

        }
        else if(index > 1U)
        {
            retVal <<= 5U;
        }


    }

    return retVal;
}

static void UpdateMax7219DisplayTime(void)
{
    if((max7219dataTransmissionState == 0U) && (newTime == 1U))
    {
        uint32_t lineData = GenerateDisplayData();
        uint8_t index = 0U;
        uint8_t shift = 24U;
        for(index = 0U; index < 4U; index++)
        {
            sendData((digits[displayLine] | ((lineData >> shift) & 0xFF)), index);
            shift -= 8;
        }

        displayLine++;

        if(displayLine > 7U)
        {
            newTime = 0U;
            displayLine = 0U;
        }
    }
}

void RunMax7219Display(void)
{
    if(initState > 3U)
    {
        UpdateMax7219DisplayTime();
    }
    else
    {
        InitMax7219Display();
    }

    SendSPIBuffer();
}
