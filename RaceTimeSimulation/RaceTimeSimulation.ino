unsigned long lastEdge = 0U;
unsigned char state = 0U;

unsigned long lastEdgeSensor1Out = 0U;
unsigned long lastEdgeSensor2Out = 0U;

void setup() {
  // put your setup code here, to run once:
  pinMode(2, OUTPUT);
  pinMode(4, OUTPUT);
  pinMode(7, OUTPUT);
  pinMode(10, INPUT);
}


void loop() {
  // put your main code here, to run repeatedly:

  if ((lastEdge + 500U) < millis())
  {
    if (state == 0U)
    {
      digitalWrite(2, HIGH);
      state = 1U;
    }
    else
    {
      digitalWrite(2, LOW);
      state = 0U;
    }
    
    lastEdge = millis();
  }

  if (digitalRead(10) == HIGH)
  {
    if ((lastEdgeSensor2Out + 5000) < millis())
    {
      digitalWrite(7, HIGH);
      lastEdgeSensor1Out = millis();
    }

    if ((lastEdgeSensor1Out + 500) < millis())
    {
      digitalWrite(7, LOW);
    }

    if ((lastEdgeSensor1Out + 12845) < millis())
    {
      digitalWrite(4, HIGH);
      lastEdgeSensor2Out = millis();
    }

    if ((lastEdgeSensor2Out + 500) < millis())
    {
      digitalWrite(4, LOW);
    }
  }
  else
  {
    if ((lastEdgeSensor1Out + 10560) < millis())
    {
      digitalWrite(7, HIGH);
      lastEdgeSensor1Out = millis();
    }

    if ((lastEdgeSensor1Out + 500) < millis())
    {
      digitalWrite(7, LOW);
    }
  }
  
  
}
