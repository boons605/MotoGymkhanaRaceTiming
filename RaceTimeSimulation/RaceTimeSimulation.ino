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
        if ((millisOnStateEntry + 5000) <= millis())
        {
          digitalWrite(7, HIGH);
          millisOnStateEntry = millis();
          dualSensorRaceState++;
        }
        break;
      }
      case 1U:
      {
        if ((millisOnStateEntry + 500) <= millis())
        {
          digitalWrite(7, LOW);
          millisOnStateEntry = millis();
          dualSensorRaceState++;
        }
        break;
      }
      case 2U:
      {
        if ((millisOnStateEntry + 18845) <= millis())
        {
          digitalWrite(4, HIGH);
          millisOnStateEntry = millis();
          dualSensorRaceState++;
        }
        break;
      }
      case 3U:
      {
        if ((millisOnStateEntry + 500) <= millis())
        {
          digitalWrite(4, LOW);
          millisOnStateEntry = millis();
          dualSensorRaceState = 0U;;
        }
        break;
      }
      
    }
  }
  else
  {
    if ((lastEdgeSensor1Out + 10560) <= millis())
    {
      digitalWrite(7, HIGH);
      lastEdgeSensor1Out = millis();
    }

    if ((lastEdgeSensor1Out + 500) <= millis())
    {
      digitalWrite(7, LOW);
    }
  }  
  
  
  
  
}
