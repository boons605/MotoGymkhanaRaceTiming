/*
 * MGBTManager.h
 *
 *  Created on: 21 Nov 2020
 *      Author: cdromke
 */

#ifndef MAIN_MGBTMANAGER_H_
#define MAIN_MGBTMANAGER_H_

#include "esp_bt.h"
#include "esp_gap_ble_api.h"

#define MAXDEVICES 64

typedef enum {
	MgBtMState_Idle = 0U,
	MgBtMState_ProcessingCommand = 1U

} ManagerState;

void InitManager(void);
void RunManager(void);
void ProcessScanResult(esp_ble_gap_cb_param_t* scanResult);



#endif /* MAIN_MGBTMANAGER_H_ */
