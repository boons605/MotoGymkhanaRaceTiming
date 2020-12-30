# ESP-IDF iBeacon demo

From welcoming people as they arrive at a sporting event to providing information about a nearby museum exhibit, iBeacon opens a new world of possibilities for location awareness, and countless opportunities for interactivity between iOS devices and iBeacon hardware.

### Menuconfig
Before compiling the demo，developers also need to configure the project:

```c
idf.py menuconfig
```
And then enter `Component config->Bluetooth->Bluedroid Enable`

Because the number of peripherals may be very large, developers can enable the **BLE Scan Duplicate Options**, the maximum number of devices in scan duplicate filter depends on the free heap size, when the cache is full, it is cleared.

### Event Processing
In the iBeacon receiver demo, the scan result will be posted to `ESP_GAP_SEARCH_INQ_RES_EVT` event:

```c
switch (scan_result->scan_rst.search_evt) {
    case ESP_GAP_SEARCH_INQ_RES_EVT:
    /* Search for BLE iBeacon Packet */
    ......
    break;
    default:
    break;
}

```
### Build and Flash

Build each project and flash it to the board, then run monitor tool to view serial output:

```
idp.py -p PORT flash monitor
```

(To exit the serial monitor, type ``Ctrl-]``.)

See the Getting Started Guide for full steps to configure and use ESP-IDF to build projects.

###Test data

0xFF 0x05 0x00 0xE6 0x63 0x01 0x00 0x12 0x34 0x01 0x02 0x03 0x04 0x05

###Extra tools

- SourceMonitor by Campwood Software: http://www.campwoodsw.com/sourcemonitor.html to measure the complexity of the code.
- Artistic Style aka AStyle: http://astyle.sourceforge.net/ to automatically format code 
