/*
 * Max7219DLDWDisplay.c
 *
 *  Created on: May 14, 2020
 *      Author: r.boonstra
 *
 *      Dual Line, Double Wide display
 */


#include "Max7219Display.h"
#include "Max7219DLDWDisplay.h"
#include "Configuration.h"
#include "stm32f1xx_ll_spi.h"
#include "stm32f1xx_ll_gpio.h"

#define DISPLAYCOUNT 8
#define LINECOUNT 2
#define LOWENERGY 1U

static const uint8_t CH[2][12][16] =
{
    {
        {0x3C, 0x3C, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0xC3, 0x3C, 0x3C, 0x00, 0x00}, // 0
        {0x0C, 0x0C, 0x3C, 0x3C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x0C, 0x3F, 0x3F, 0x00, 0x00}, // 1
        {0x3C, 0x3C, 0xC3, 0xC3, 0x03, 0x03, 0x0C, 0x0C, 0xC0, 0xC0, 0xC0, 0xC0, 0xFF, 0xFF, 0x00, 0x00}, // 2
        {0x3C, 0x3C, 0xC3, 0xC3, 0x03, 0x03, 0x3C, 0x3C, 0x03, 0x03, 0xC3, 0xC3, 0x3C, 0x3C, 0x00, 0x00}, // 3
        {0x03, 0x03, 0x0F, 0x0F, 0x33, 0x33, 0xC3, 0xC3, 0xFF, 0xFF, 0x03, 0x03, 0x03, 0x03, 0x00, 0x00}, // 4
        {0xFF, 0xFF, 0xC0, 0xC0, 0xFC, 0xFC, 0x03, 0x03, 0x03, 0x03, 0xC3, 0xC3, 0x3C, 0x3C, 0x00, 0x00}, // 5
        {0x3C, 0x3C, 0xC0, 0xC0, 0xC0, 0xC0, 0x3C, 0x3C, 0xC3, 0xC3, 0xC3, 0xC3, 0x3C, 0x3C, 0x00, 0x00}, // 6
        {0xFF, 0xFF, 0x03, 0x03, 0x06, 0x06, 0x0C, 0x0C, 0x18, 0x18, 0x30, 0x30, 0x60, 0x60, 0x00, 0x00}, // 7
        {0x3C, 0x3C, 0xC3, 0xC3, 0xC3, 0xC3, 0x3C, 0x3C, 0xC3, 0xC3, 0xC3, 0xC3, 0x3C, 0x3C, 0x00, 0x00}, // 8
        {0x3C, 0x3C, 0xC3, 0xC3, 0xC3, 0xC3, 0x3C, 0x3C, 0x03, 0x03, 0x03, 0x03, 0x3C, 0x3C, 0x00, 0x00}, // 9
        {0x00, 0x00, 0x00, 0x00, 0x06, 0x06, 0x00, 0x00, 0x00, 0x00, 0x06, 0x06, 0x00, 0x00, 0x00, 0x00}, // :
        {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x03} // .
    },
    {
        {0xFF, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0xFF}, // 0
        {0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01}, // 1
        {0xFF, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0xFF, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0xFF}, // 2
        {0xFF, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0xFF}, // 3
        {0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0xFF, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01}, // 4
        {0xFF, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0xFF, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0xFF}, // 5
        {0xFF, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0xFF, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0xFF}, // 6
        {0xFF, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01}, // 7
        {0xFF, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0xFF, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0xFF}, // 8
        {0xFF, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0x81, 0xFF, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0xFF}, // 9
        {0x00, 0x00, 0x00, 0x00, 0x04, 0x04, 0x00, 0x00, 0x00, 0x00, 0x04, 0x04, 0x00, 0x00, 0x00, 0x00}, // :
        {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01} // .
    }
};

static const uint16_t maxtrixLines[] =
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

static const uint16_t max7219InitActions[] =
{
	REG_NO_OP		| 0x00,
	REG_DIGIT_0 	| 0x00,
	REG_DIGIT_1 	| 0x00,
	REG_DIGIT_2 	| 0x00,
	REG_DIGIT_3 	| 0x00,
	REG_DIGIT_4 	| 0x00,
	REG_DIGIT_5 	| 0x00,
	REG_DIGIT_6 	| 0x00,
	REG_DIGIT_7 	| 0x00,
	REG_DECODE_MODE | 0x00,
	REG_INTENSITY 	| 0x00,
	REG_SCAN_LIMIT 	| 0x00,
	REG_SHUTDOWN 	| 0x00,
	REG_DISPLAY_TEST| 0x00,
	REG_SHUTDOWN | 0x01,
    REG_DECODE_MODE | 0x00,
    REG_SCAN_LIMIT | 0x07,
    REG_INTENSITY | DISPLAYBRIGHTNESS
};

static SPI_TypeDef* spiBus[LINECOUNT] = {SPI2, SPI1};
static GPIO_TypeDef* csGpio[LINECOUNT] = {GPIOB, GPIOA};
static const uint32_t csPin[LINECOUNT] = {LL_GPIO_PIN_12, LL_GPIO_PIN_4};

static uint16_t max7219SpiBuffer[LINECOUNT][DISPLAYCOUNT] = {0U };
static uint8_t max7219dataIndex[LINECOUNT] = { 0U };
static uint8_t max7219dataTransmissionState[LINECOUNT] = { 0U };
static uint8_t initState[LINECOUNT] = { 0U} ;
static uint8_t newTime = 0U;
static uint32_t timeDataBCD = 0U;
static uint8_t characterLine = 0U;
static uint8_t initDone = 0U;


static void sendData(uint16_t data, uint8_t index, uint8_t line)
{
    max7219SpiBuffer[line][index] = data;
    if(max7219dataTransmissionState[line] == 0U)
    {
        max7219dataTransmissionState[line] = 1U;
    }
}


static void SendSPIBuffer(uint8_t line)
{
    LL_GPIO_ResetOutputPin(csGpio[line], csPin[line]);
    switch(max7219dataTransmissionState[line])
    {
        case 1U:
        {
            if(max7219dataIndex[line] < DISPLAYCOUNT)
            {
                if(LL_SPI_IsActiveFlag_TXE(spiBus[line]))
                {
                    LL_SPI_TransmitData16(spiBus[line], max7219SpiBuffer[line][max7219dataIndex[line]]);
                    max7219dataIndex[line]++;
                }
            }
            else
            {
                max7219dataIndex[line] = 0U;
                max7219dataTransmissionState[line]++;
            }
            break;
        }
        case 2U:
        {
            if(LL_SPI_IsActiveFlag_BSY(spiBus[line]) == 0U)
            {
                LL_GPIO_SetOutputPin(csGpio[line], csPin[line]);
                max7219dataTransmissionState[line] = 0U;
                LL_GPIO_ResetOutputPin(csGpio[line], csPin[line]);
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



void UpdateMax7219DLDWDisplay(uint32_t data)
{
    timeDataBCD = data;
    //timeDataBCD = 0x000789123;
    newTime = 1U;
    characterLine = 0U;
}

static void InitMax7219DLDWDisplay(void)
{
    uint8_t line = 0U;
    for(line = 0U; line < LINECOUNT; line++)
    {
        if(max7219dataTransmissionState[line] == 0U)
        {
            if(initState[line] < (sizeof(max7219InitActions) / sizeof(uint16_t)))
            {
            	if (initState[line] == 0U)
				{
					LL_GPIO_SetOutputPin(csGpio[line], csPin[line]);
				}

                uint8_t index = 0U;
                for(index = 0U; index < DISPLAYCOUNT; index++)
                {
                    sendData(max7219InitActions[initState[line]], index, line);
                }
                initState[line]++;

            }
        }
    }

    initDone = 1U;
    for(line = 0U; line < LINECOUNT; line++)
    {
        if(initState[line] < (sizeof(max7219InitActions) / sizeof(uint16_t)))
        {
            initDone = 0U;
        }
    }
}

static void GenerateDisplayData(uint32_t* minSec, uint32_t* millis, uint8_t displayLineNo)
{
    uint8_t index = 0U;
    uint8_t characterLineIndex = characterLine + (8U * displayLineNo);

    for(index = 6U; index > 0U; index--)
    {
        uint32_t* retVal;
        if(index < 4U)
        {
            retVal = millis;
        }
        else
        {
            retVal = minSec;
        }

        uint8_t digit = ((timeDataBCD >> ((index - 1) * 4)) & 0x0F);
        if(digit < 10U)
        {
            if((digit != 0U) || (*retVal != 0U) || (index < 4U))
            {
                *retVal |= CH[LOWENERGY][digit][characterLineIndex];
            }
        }

        if(index == 6U)
        {
            *retVal <<= 4U;
            if(*retVal != 0U)
            {
                *retVal |= CH[LOWENERGY][10U][characterLineIndex];;
            }
            *retVal <<= 8U;
        }
        else if(index == 4U)
        {
            *retVal <<= 3U;
            *retVal |= CH[LOWENERGY][11U][characterLineIndex];
            //*retVal <<= 8U;
        }
        else if(index > 1U)
        {
            *retVal <<= 9U;
        }


    }

}

static void UpdateMax7219DLDWDisplayTime(void)
{
    uint8_t lineIndex = 0U;
    uint8_t readyForNextTransmission = 1U;
    for(lineIndex = 0U; lineIndex < LINECOUNT; lineIndex++)
    {
        if(max7219dataTransmissionState[lineIndex] != 0U)
        {
            readyForNextTransmission = 0U;
        }
    }

    if((readyForNextTransmission == 1U) && (newTime == 1U))
    {
        for(lineIndex = 0U; lineIndex < LINECOUNT; lineIndex++)
        {
            uint32_t lineDataMinSec = 0U;
            uint32_t lineDataMillis = 0U;
            GenerateDisplayData(&lineDataMinSec, &lineDataMillis, lineIndex);
            uint8_t index = 0U;
            uint8_t shift = 24U;
            uint32_t* lineData = &lineDataMinSec;
            for(index = 0U; index < DISPLAYCOUNT; index++)
            {
                sendData((maxtrixLines[characterLine] | (((*lineData) >> shift) & 0xFF)), index, lineIndex);
                if(shift == 0U)
                {
                    shift = 24U;
                    lineData = &lineDataMillis;
                }
                else
                {
                    shift -= 8;
                }
            }
        }
        characterLine++;

        if(characterLine > 7U)
        {
            newTime = 0U;
            characterLine = 0U;
        }
    }
}

void RunMax7219DLDWDisplay(void)
{

    if(initDone == 1U)
    {
        UpdateMax7219DLDWDisplayTime();
    }
    else
    {
        InitMax7219DLDWDisplay();
    }

    uint8_t index = 0U;
    for(index = 0U; index < LINECOUNT; index++)
    {
        SendSPIBuffer(index);
    }

}
