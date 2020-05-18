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


  if (digitalRead(10) == HIGH)
  {
    switch (dualSensorRaceState)
    { 
      case 0U:
      {
        if ((millisOnStateEntry + 5000000) <= micros())
        {
          digitalWrite(7, HIGH);
          millisOnStateEntry = micros();
          dualSensorRaceState++;
        }
        break;
      }
      case 1U:
      {
        if ((millisOnStateEntry + 500000) <= micros())
        {
          digitalWrite(7, LOW);
          millisOnStateEntry = micros();
          dualSensorRaceState++;
        }
        break;
      }
      case 2U:
      {
        if ((millisOnStateEntry + 18845000) <= micros())
        {
          digitalWrite(4, HIGH);
          millisOnStateEntry = micros();
          dualSensorRaceState++;
        }
        break;
      }
      case 3U:
      {
        if ((millisOnStateEntry + 500000) <= micros())
        {
          digitalWrite(4, LOW);
          millisOnStateEntry = micros();
          dualSensorRaceState = 0U;;
        }
        break;
      }
      
    }
  }
  else
  {
    if ((lastEdgeSensor1Out + 10560000) <= micros())
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
