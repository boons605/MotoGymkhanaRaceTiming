unsigned long lastEdge = 0U;
unsigned char state = 0U;

unsigned long lastEdgeSensor1Out = 0U;

unsigned long dualSensorRaceState = 0U;
unsigned long millisOnStateEntry = 0U;

void setup() {
  // put your setup code here, to run once:
  pinMode(2, OUTPUT);
  pinMode(4, OUTPUT);
  pinMode(7, OUTPUT);
  pinMode(10, INPUT);
}


void loop() {
  // put your main code here, to run repeatedly:


  /*if (digitalRead(10) == HIGH)
  {
    switch (dualSensorRaceState)
    { 
      case 0U:
      {
        if (millisOnStateEntry <= micros())
        {
          digitalWrite(7, HIGH);
          millisOnStateEntry = micros() + 500000;
          dualSensorRaceState++;
        }
        break;
      }
      case 1U:
      {
        if ((millisOnStateEntry) <= micros())
        {
          digitalWrite(7, LOW);
          millisOnStateEntry = micros() + 18845000;
          dualSensorRaceState++;
        }
        break;
      }
      case 2U:
      {
        if ((millisOnStateEntry) <= micros())
        {
          digitalWrite(4, HIGH);
          millisOnStateEntry = micros() + 500000;
          dualSensorRaceState++;
        }
        break;
      }
      case 3U:
      {
        if (millisOnStateEntry <= micros())
        {
          digitalWrite(4, LOW);
          millisOnStateEntry = micros() + 5000000;
          dualSensorRaceState = 0U;
        }
        break;
      }
      
    }
  }
  else*/
  {
    if ((lastEdgeSensor1Out + 13260000) <= micros())
    {
      digitalWrite(7, HIGH);
      lastEdgeSensor1Out = micros();
    }

    if ((lastEdgeSensor1Out + 500000) <= micros())
    {
      digitalWrite(7, LOW);
    }
  }  
  
  
  
  
}
