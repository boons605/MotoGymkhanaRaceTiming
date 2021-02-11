EESchema Schematic File Version 4
EELAYER 30 0
EELAYER END
$Descr A4 11693 8268
encoding utf-8
Sheet 1 1
Title ""
Date ""
Rev ""
Comp ""
Comment1 ""
Comment2 ""
Comment3 ""
Comment4 ""
$EndDescr
$Comp
L SparkFun-RF:XBEE JP1
U 1 1 601EE360
P 5950 4350
F 0 "JP1" H 5950 5110 45  0000 C CNN
F 1 "XBEE" H 5950 5026 45  0000 C CNN
F 2 "XBEE" H 5950 4950 20  0001 C CNN
F 3 "" H 5950 4350 50  0001 C CNN
F 4 "XXX-00000" H 5950 4931 60  0000 C CNN "Field4"
	1    5950 4350
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR?
U 1 1 601EFD45
P 5250 4850
F 0 "#PWR?" H 5250 4600 50  0001 C CNN
F 1 "GND" H 5255 4677 50  0000 C CNN
F 2 "" H 5250 4850 50  0001 C CNN
F 3 "" H 5250 4850 50  0001 C CNN
	1    5250 4850
	1    0    0    -1  
$EndComp
$Comp
L Connector_Generic:Conn_01x19 J2
U 1 1 601F4B48
P 5700 2050
F 0 "J2" V 5825 2046 50  0000 C CNN
F 1 "Conn_01x19" V 5916 2046 50  0000 C CNN
F 2 "Connector_PinSocket_2.54mm:PinSocket_1x19_P2.54mm_Vertical" H 5700 2050 50  0001 C CNN
F 3 "~" H 5700 2050 50  0001 C CNN
	1    5700 2050
	0    1    1    0   
$EndComp
$Comp
L Connector_Generic:Conn_01x19 J1
U 1 1 601F7BE1
P 5700 2600
F 0 "J1" V 5917 2596 50  0000 C CNN
F 1 "Conn_01x19" V 5826 2596 50  0000 C CNN
F 2 "Connector_PinSocket_2.54mm:PinSocket_1x19_P2.54mm_Vertical" H 5700 2600 50  0001 C CNN
F 3 "~" H 5700 2600 50  0001 C CNN
	1    5700 2600
	0    -1   -1   0   
$EndComp
$Comp
L Connector_Generic:Conn_01x06 J?
U 1 1 601FF8D6
P 2250 2300
F 0 "J?" H 2168 1775 50  0000 C CNN
F 1 "Conn_01x06" H 2168 1866 50  0000 C CNN
F 2 "Connector_PinSocket_2.54mm:PinSocket_1x06_P2.54mm_Horizontal" H 2250 2300 50  0001 C CNN
F 3 "~" H 2250 2300 50  0001 C CNN
	1    2250 2300
	-1   0    0    1   
$EndComp
$Comp
L Transistor_Array:ULN2003A U?
U 1 1 602574F9
P 8300 3800
F 0 "U?" H 8300 4467 50  0000 C CNN
F 1 "ULN2003A" H 8300 4376 50  0000 C CNN
F 2 "" H 8350 3250 50  0001 L CNN
F 3 "http://www.ti.com/lit/ds/symlink/uln2003a.pdf" H 8400 3600 50  0001 C CNN
	1    8300 3800
	1    0    0    -1  
$EndComp
$Comp
L power:GND #PWR?
U 1 1 60258D0D
P 8300 4400
F 0 "#PWR?" H 8300 4150 50  0001 C CNN
F 1 "GND" H 8305 4227 50  0000 C CNN
F 2 "" H 8300 4400 50  0001 C CNN
F 3 "" H 8300 4400 50  0001 C CNN
	1    8300 4400
	1    0    0    -1  
$EndComp
$EndSCHEMATC
