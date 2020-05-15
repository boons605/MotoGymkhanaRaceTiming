/*
 * Max7219Display.c
 *
 *  Created on: May 14, 2020
 *      Author: r.boonstra
 */

#include "Max7219Display.h"
#include "stm32f1xx_ll_spi.h"
#include "stm32f1xx_ll_gpio.h"

#define DISPLAYCOUNT 4

const uint8_t CH[12][7] = {
  {4, 8, 0b00111110, 0b01000001, 0b01000001, 0b00111110, 0b00000000}, // 0
  {3, 8, 0b01000010, 0b01111111, 0b01000000, 0b00000000, 0b00000000}, // 1
  {4, 8, 0b01100010, 0b01010001, 0b01001001, 0b01000110, 0b00000000}, // 2
  {4, 8, 0b00100010, 0b01000001, 0b01001001, 0b00110110, 0b00000000}, // 3
  {4, 8, 0b00011000, 0b00010100, 0b00010010, 0b01111111, 0b00000000}, // 4
  {4, 8, 0b00100111, 0b01000101, 0b01000101, 0b00111001, 0b00000000}, // 5
  {4, 8, 0b00111110, 0b01001001, 0b01001001, 0b00110000, 0b00000000}, // 6
  {4, 8, 0b01100001, 0b00010001, 0b00001001, 0b00000111, 0b00000000}, // 7
  {4, 8, 0b00110110, 0b01001001, 0b01001001, 0b00110110, 0b00000000}, // 8
  {4, 8, 0b00000110, 0b01001001, 0b01001001, 0b00111110, 0b00000000}, // 9
  {2, 8, 0b01010000, 0b00000000, 0b00000000, 0b00000000, 0b00000000}, // :
  {2, 8, 0b01000000, 0b00000000, 0b00000000, 0b00000000, 0b00000000}// .
};

const uint16_t digits[] = {
		REG_DIGIT_0,
		REG_DIGIT_1,
		REG_DIGIT_2,
		REG_DIGIT_3,
		REG_DIGIT_4,
		REG_DIGIT_5,
		REG_DIGIT_6,
		REG_DIGIT_7
};

const uint16_t max7219InitActions[4] = {
		REG_SHUTDOWN | 0x01,
		REG_DECODE_MODE | 0x00,
		REG_SCAN_LIMIT | 0x07,
		REG_INTENSITY | 0x0F
};

static uint16_t max7219SpiBuffer[DISPLAYCOUNT] = {0};
static uint8_t max7219dataIndex = 0U;
static uint8_t max7219dataTransmissionState = 0U;
static uint8_t initState = 0U;
static uint8_t newTime = 0U;
static uint32_t timeDataBCD = 0U;
static uint8_t displayLine = 0U;


static void sendData(uint16_t data, uint8_t index) {
	max7219SpiBuffer[index] = data;
	if (max7219dataTransmissionState == 0U)
	{
		max7219dataTransmissionState = 1U;
	}
}


static void SendSPIBuffer(void)
{
	LL_GPIO_ResetOutputPin(GPIOB, LL_GPIO_PIN_12);
	switch (max7219dataTransmissionState)
	{
		case 1U:
		{
			if (max7219dataIndex < DISPLAYCOUNT)
			{
				if (LL_SPI_IsActiveFlag_TXE(SPI2))
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
			if (LL_SPI_IsActiveFlag_BSY(SPI2) == 0U)
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
	newTime = 1U;
}

void InitMax7219Display(void)
{
	if (max7219dataTransmissionState == 0U)
	{
		uint8_t index = 0U;
		for (index = 0U; index < 4; index++)
		{
			sendData(max7219InitActions[initState], index);
		}
		initState++;
	}
}

static uint8_t GetCharBit(uint8_t digit, uint8_t digitPosition)
{
	return ((CH[digit][digitPosition+2] >> (7U-displayLine)) & 0x01);
}

//I'm not gonna be proud of this code. There must be a smarter way.
static void UpdateDisplayLineDisplay1(void)
{
	uint8_t index = 0U;
	uint16_t data = digits[displayLine];
	for (index = 0U; index < 8; index++)
	{
		if (index < 4)
		{
			data |= (GetCharBit(((timeDataBCD >> 4) & 0x0F), index) << index);
		}
		else if (index > 4)
		{
			data |= (GetCharBit((timeDataBCD & 0x0F), index-5) << index);
		}
	}
	sendData(data, 0U);
}

static void UpdateDisplayLineDisplay2(void)
{
	uint8_t index = 0U;
	uint16_t data = digits[displayLine];
	for (index = 0U; index < 8; index++)
	{
		if (index < 1)
		{
			data |= (GetCharBit(11, index+3) << index);
		}
		else if ((index > 1) && (index < 6))
		{
			data |= (GetCharBit(((timeDataBCD >> 8) & 0x0F), index-2) << index);
		}
		else if (index > 6)
		{
			data |= (GetCharBit(((timeDataBCD >> 4) & 0x0F), index-7) << index);;
		}
	}
	sendData(data, 1U);
}

static void UpdateDisplayLineDisplay3(void)
{
	uint8_t index = 0U;
	uint16_t data = digits[displayLine];
	for (index = 0U; index < 8; index++)
	{
		if (index < 4)
		{
			data |= (GetCharBit(((timeDataBCD >> 20) & 0x0F), index) << index);
		}
		else if (index > 4)
		{
			data |= (GetCharBit(((timeDataBCD >> 16) & 0x0F), index-5) << index);
		}
	}
	sendData(data, 2U);
}

static void UpdateDisplayLineDisplay4(void)
{
	uint8_t index = 0U;
	uint16_t data = digits[displayLine];
	for (index = 0U; index < 8; index++)
	{
		if (index < 1)
		{
			data |= (GetCharBit(((timeDataBCD >> 24) & 0x0F), index+3) << index);
		}
		else if ((index > 1) && (index < 4))
		{
			data |= (GetCharBit(10, index-2) << index);
		}
		else
		{
			data |= (GetCharBit(((timeDataBCD >> 20) & 0x0F), index-2) << index);
		}
	}
	sendData(data, 3U);
}

static void UpdateMax7219DisplayTime(void)
{
	if ((max7219dataTransmissionState == 0U) && (newTime == 1U))
	{
		UpdateDisplayLineDisplay1();
		UpdateDisplayLineDisplay2();
		UpdateDisplayLineDisplay3();
		UpdateDisplayLineDisplay4();
		displayLine++;

		if (displayLine > 7U)
		{
			newTime = 0U;
			displayLine = 0U;
		}
	}
}

void RunMax7219Display(void)
{
	if (initState > 3U)
	{
		UpdateMax7219DisplayTime();
	}
	else
	{
		InitMax7219Display();
	}

	SendSPIBuffer();
}
