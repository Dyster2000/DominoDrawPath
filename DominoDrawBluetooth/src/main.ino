/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
 */
#include <Arduino.h>
#include "StatusHandler.h"
#include "DominoBleServer.h"
#include "DominoData.h"
#include <SparkFun_TB6612.h> //motor driver library
#include <ESP32Servo.h>      //servo controller library
#include <OneButton.h>       //button handling library

#define SERVO_RIGHT 20 // PWM value to make the servo move right
#define SERVO_LEFT 180 // PWM value to make the servo move left.
#define SERVO_PIN 13   // PWM pin for domino dispensing servo
#define AIN1 9         // motor driver
#define BIN1 7         // motor driver
#define AIN2 10        // motor driver
#define BIN2 6         // motor driver
#define PWMA 11        // motor driver
#define PWMB 5         // motor driver
#define STBY 8         // motor driver
#define OFFSET_A 1     // Switch to -1 if the motor is running backwards
#define OFFSET_B 1     // Switch to -1 if the motor is running backwards
#define BUTTON_PIN 12  // The pin the switch for detecting domino outage is connected to.
#define TOP_SPEED 90   // PWM value for max speed of motors

constexpr uint32_t MM_PER_SEC = 34; // How far robot travels per ms per combined speed value - about 34 mm/s @ 180 combined pwm
constexpr uint32_t NM_PER_MS = MM_PER_SEC * 1000 / 180; // How far robot travels per ms per combined speed value - about 34 mm/s @ 180 combined pwm
constexpr uint32_t DISPENSE_MM = 35;  // 35mm

DominoData data;
DominoBleServer bleServer(data);

Servo servoMotor;
Motor motorRight(AIN1, AIN2, PWMA, OFFSET_A, STBY);
Motor motorLeft(BIN1, BIN2, PWMB, OFFSET_B, STBY);
uint32_t combinedmotorspeed = 0;

// Out of dominoes switch setup
OneButton emptyButton(BUTTON_PIN, false, false); // Active high, no internal pullup resistor

// Distance based domino dropping variables - track as ms since that is what we can measure
uint32_t distanceSinceLastDrop = 0;
bool dominoDropped = false;
bool lastDominoDropped = false;
uint32_t RemainingDistanceTraveledNM = 0;

uint32_t lastTimeUS = 0; // Last loop time in uSec

void setup()
{
  Serial.begin(115200);

  // Setup domino robot
  servoMotor.attach(SERVO_PIN); // Attach the servo to the specified pin
  servoMotor.write(SERVO_LEFT); // Set initial servo position to the left to pick up a domino for immediate dispensing

  // Set up the button handler to detect long press of limit switch.
  // Have to use longPress because switch is pressed and held, not pressed and release as a single click.
  emptyButton.setPressMs(200); // Sets up the button handler to wait for 200ms before reporting the limit switch is pressed.
  emptyButton.attachLongPressStart([]()
                                   {
    // Set flag when empty switch is triggered
    if (!data.IsEmpty)
    {
      data.IsEmpty = true;
    } });
  emptyButton.attachLongPressStop([]()
                                  {
    // Set flag when empty switch is triggered
    if (data.IsEmpty)
    {
      data.IsEmpty = false;
      lastDominoDropped = false;
    } });

  // Setup BLE (bluetooth low energy)
  bleServer.Init();
  Serial.println("Waiting for DominoPathDraw app to connect...");
}

bool Moving = true;

uint32_t debugSec = 0;
uint32_t debugCnt = 0;
uint32_t seconds = 0;

void loop()
{
  uint32_t curTimeUS = micros();
  uint32_t deltaTimeUS = curTimeUS - lastTimeUS;
  uint32_t lastTimeUS_Save = lastTimeUS;
  lastTimeUS = curTimeUS;

  bleServer.Loop(deltaTimeUS);

  // Check if we need to stop based on switch and control flags.
  emptyButton.tick();
  if (data.IsEmpty && data.Moving && data.StopOnEmpty && lastDominoDropped && distanceSinceLastDrop >= DISPENSE_MM)
  {
    // When data.IsEmpty is triggered, we have just loaded the last one. Travel till it is dropped plus a little more.
    data.Moving = false;
    Serial.println("Stop moving, out of dominoes");
  }

  static int lastLeftMotorSpeed = 0;
  static int lastRightMotorSpeed = 0;

  // Handle movement
  if (data.Moving)
  {
    /*if (debugCnt < 10)
    {
      debugCnt++;
      Serial.print("curTimeUS=");
      Serial.print(curTimeUS);
      Serial.print(", curTimeMS=");
      Serial.println(curTimeMS);
    }*/
    // Calculate motor speeds based on the control signal
    int leftMotorSpeed = TOP_SPEED + data.Direction;
    int rightMotorSpeed = TOP_SPEED - data.Direction;

    // Ensure motor speeds are within the valid range
    leftMotorSpeed = constrain(leftMotorSpeed, 0, TOP_SPEED); // This makes the speed be somewhere between 0 and TOP_SPEED
    rightMotorSpeed = constrain(rightMotorSpeed, 0, TOP_SPEED);

    if (leftMotorSpeed != lastLeftMotorSpeed || rightMotorSpeed != lastRightMotorSpeed)
    {
      /*Serial.print("Left speed=");
      Serial.print(leftMotorSpeed);
      Serial.print(", Right speed=");
      Serial.print(rightMotorSpeed);
      Serial.print(", Dir=");
      Serial.println(data.Direction);*/
      lastLeftMotorSpeed = leftMotorSpeed;
      lastRightMotorSpeed = rightMotorSpeed;

      motorLeft.drive(leftMotorSpeed);
      motorRight.drive(rightMotorSpeed);
    }

    // Apply motor speeds
    uint32_t combinedmotorspeed = leftMotorSpeed + rightMotorSpeed;
    uint32_t distanceNM = combinedmotorspeed * deltaTimeUS * NM_PER_MS / 1000UL; // TMP
    RemainingDistanceTraveledNM += combinedmotorspeed * deltaTimeUS * NM_PER_MS / 1000;
    uint32_t distanceMM = RemainingDistanceTraveledNM / 1000000UL;
    RemainingDistanceTraveledNM -= distanceMM * 1000000UL;
    // DistanceTraveledNM += distanceUM;
    data.DistanceTraveledMM += distanceMM;
    distanceSinceLastDrop += distanceMM;

    /*uint32_t curSec = curTimeUS / 1000000;
    if (debugSec != curSec)
    {
      debugSec = curSec;
      //debugCnt = 0;
      seconds++;

      //Serial.print("curTimeUS=");
      //Serial.print(curTimeUS);
      //Serial.print(", lastTimeUS=");
      //Serial.print(lastTimeUS_Save);
      Serial.print("deltaTimeUS=");
      Serial.print(deltaTimeUS);
      //Serial.print(", combinedmotorspeed=");
      //Serial.print(combinedmotorspeed);
      //Serial.print(", seconds=");
      //Serial.print(seconds);
      //uint32_t test1 = combinedmotorspeed * deltaTimeUS;
      //Serial.print(", test1=");
      //Serial.print(test1);
      //Serial.print(", distanceUM=");
      //Serial.print(distanceUM);
      //Serial.print(", distanceUM=");
      //Serial.print(distanceUM);

      Serial.print("distanceNM=");
      Serial.print(distanceNM);
      Serial.print("DistanceTraveledMM=");
      Serial.print(data.DistanceTraveledMM);
      Serial.print(", RemainingDistanceTraveledNM=");
      Serial.print(RemainingDistanceTraveledNM);
      int32_t diff = (int32_t)MM_PER_SEC * (seconds - 1) - (int32_t)data.DistanceTraveledMM;
      Serial.print(", diff=");
      Serial.println(diff);
    }*/

    // Handle domino dispensing
    if (data.Dispensing)
    {
      // figure out when to dispense a domino based on how far the robot has travelled.
      if ((distanceSinceLastDrop >= DISPENSE_MM) && (!dominoDropped) && (!lastDominoDropped))
      { // if we've travelled far enough and a domino hasn't been dropped yet
        servoMotor.write(SERVO_RIGHT);
        distanceSinceLastDrop = 0; // reset drop distance
        dominoDropped = true;      // domino has been dropped
        if (data.IsEmpty)
          lastDominoDropped = true;
      }
      else if ((distanceSinceLastDrop >= 0.7 * DISPENSE_MM) && (dominoDropped))
      {                               // if a domino was dropped and we're far enough away to close the domino gate without jamming
        servoMotor.write(SERVO_LEFT); // pick up a new domino for dropping
        dominoDropped = false;        // we haven't dropped the new domino, so now this is false
      }
    }
    else
      lastDominoDropped = true;
  }
  else
  {
    if (lastLeftMotorSpeed != 0 || lastRightMotorSpeed != 0)
    {
      Serial.println("Brake");
      motorRight.brake();
      motorLeft.brake();
      lastLeftMotorSpeed = 0;
      lastRightMotorSpeed = 0;
    }
  }
}
