/*
 * Display.h
 *
 *  Created on: May 10, 2020
 *      Author: r.boonstra
 */

#ifndef INC_DISPLAY_H_
#define INC_DISPLAY_H_

#include <stdint.h>

typedef enum
{
    DTEA_ClearDisplay = 0U,
    DTEA_ShowRunningTime = 1U
} DisplayTimeExpiredAction;

void UpdateDisplay(uint32_t newTimeInMs, uint32_t displayDurationInMs, DisplayTimeExpiredAction whatsNext);
void RunDisplay(void);
void ResetRunningDisplayTime(uint32_t startTime);

#endif /* INC_DISPLAY_H_ */




