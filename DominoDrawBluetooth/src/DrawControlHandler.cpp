/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/
#include "DrawControlHandler.h"
#include "DominoBleServer.h"
#include "DominoData.h"
#include <Arduino.h>
#include <BLEUtils.h>
#include <BLE2902.h>

DrawControlHandler::DrawControlHandler(DominoData &data, DominoBleServer &server, BLEService &service)
    : m_Data(data)
    , m_Server(server)
    , m_ReceivedDataUpdated(false)
    , m_CurrentIndex(0)
    , m_DistanceForNextStep(0)
    , m_TargetAngle(0)
    , m_StepElapsedTimeUS(0)
{
  // Create a BLE Characteristic
  m_pCharacteristic = service.createCharacteristic(
      CHARACTERISTIC_UUID,
      BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_WRITE);

  // Create a BLE Descriptor
  m_pCharacteristic->addDescriptor(new BLE2902());

  m_pCharacteristic->setCallbacks(this);
}

void DrawControlHandler::Loop(uint32_t deltaTimeUS)
{
  if (m_ReceivedDataUpdated)
  {
    /*Serial.print("[DrawControlHandler::Loop] Got Points: Len=");
    Serial.println(m_Points.size());
    Serial.print("Data: ");
    for (uint16_t i = 0; i < m_Points.size(); i++)
    {
      if (i > 0)
        Serial.print(", ");
      Serial.print("(");
      Serial.print(m_Points[i].X);
      Serial.print(",");
      Serial.print(m_Points[i].Y);
      Serial.print(")");
    }*/
    if (m_Steps.size() > 0)
    {
      Serial.print("[DrawControlHandler::Loop] Setup new path: 0/");
      Serial.println(m_Steps.size());
      m_CurrentIndex = 0;
      SetNextStep();

      m_PathActive = true;
    }
    m_ReceivedDataUpdated = false;
  }

  if (!m_Data.ManualMode)
  {
    if (m_Data.Moving && !m_PathActive)
    {
      // Can't move in draw mode without a path
      m_Data.Moving = false;
      Serial.println("[DrawControlHandler::Loop] Draw mode can't move without a path");
    }

    // Check updating of direction based on path
    if (m_Data.Moving)
    {
      m_StepElapsedTimeUS += deltaTimeUS;
      if (m_Data.DistanceTraveledMM >= m_DistanceForNextStep)
      {
        m_CurrentIndex++;
        if (m_CurrentIndex == m_Steps.size())
        {
          Serial.println("[DrawControlHandler::Loop] At end of path, stop now");
          m_PathActive = false;
          m_Data.Moving = false;
        }
        else
          SetNextStep();
      }
      else if (m_Data.Direction != 0)
      {
        /*static int debugCnt = 0;

        if (m_Data.Direction > 10 || m_Data.Direction < -10)
        {
          if (debugCnt++ < 30)
          {
            Serial.print("deltaTimeUS=");
            Serial.print(deltaTimeUS);
            Serial.print(", Direction=");
            Serial.print(m_Data.Direction);
            Serial.print(", CurrentAngle=");
            Serial.print(m_CurrentAngle);
            float adj = MicrosPerTurnDegree * deltaTimeUS * m_Data.Direction;
            float newCurrentAngle = m_CurrentAngle + adj;
            Serial.print(", NewCurrentAngle=");
            Serial.println(newCurrentAngle);
          }
        }*/

        m_CurrentAngle -= MicrosPerTurnDegree * deltaTimeUS * m_Data.Direction;

        float diff = fabs(m_TargetAngle - m_CurrentAngle);
        if (diff > m_LastDiff)
        {
          Serial.print("[DrawControlHandler::Loop] Reached angle ");
          Serial.print(m_CurrentAngle);
          Serial.print(", Update direction to center at ");
          Serial.print(m_StepElapsedTimeUS);
          Serial.println(" us");

          m_CurrentAngle = m_TargetAngle;
          m_Data.Direction = 0;
        }
        else
          m_LastDiff = diff;
      }
    }
  }
}

void DrawControlHandler::SetNextStep()
{
  m_DistanceForNextStep = m_Data.DistanceTraveledMM + m_Steps[m_CurrentIndex].DistanceMM; // Set distance to next step
  m_TargetAngle = m_Steps[m_CurrentIndex].Angle;
  if (m_CurrentIndex == 0)
  {
    m_CurrentAngle = m_Steps[0].Angle; // Treat robot as pointing in the angle of the first step
    m_Data.Direction = 0;
  }
  else
  {
    auto angleDiff = m_CurrentAngle - m_TargetAngle;
    if (angleDiff > 180)
      angleDiff -= 360;
    else if (angleDiff < -180)
      angleDiff += 360;
    if (m_DistanceForNextStep < 5)
      angleDiff *= 3;
    else if (m_DistanceForNextStep < 10)
      angleDiff *= 2;
    //auto distAdjust = m_DistanceForNextStep;
    angleDiff = constrain(angleDiff, -80, 80);
    m_Data.Direction = (int8_t)angleDiff;
    m_LastDiff = fabs(m_TargetAngle - m_CurrentAngle);
  }

  Serial.print("[DrawControlHandler::Loop] Change to step: ");
  Serial.print(m_StepElapsedTimeUS);
  Serial.print(" us: ");
  Serial.print(m_CurrentIndex);
  Serial.print("/");
  Serial.print(m_Steps.size());
  Serial.print(": dir=");
  Serial.print(m_Data.Direction);
  Serial.print(", distMM=");
  Serial.print(m_Steps[m_CurrentIndex].DistanceMM);

  Serial.print(", m_TargetAngle=");
  Serial.print(m_TargetAngle);
  Serial.print(", m_CurrentAngle=");
  Serial.print(m_CurrentAngle);
  Serial.print(", Direction=");
  Serial.println(m_Data.Direction);

  /*Serial.print(", DistanceTraveled=");
  Serial.print(m_Data.DistanceTraveledMM);
  Serial.print(", DistanceForNextStep=");
  Serial.print(m_DistanceForNextStep);
  Serial.print(", distMM=");
  Serial.println(m_Steps[m_CurrentIndex].DistanceMM);*/

  m_StepElapsedTimeUS = 0;
}

void DrawControlHandler::onWrite(BLECharacteristic *pCharacteristic, esp_ble_gatts_cb_param_t *param)
{
  if ((m_pCharacteristic->getLength() >= MinSize) && (m_pCharacteristic->getLength() <= MaxSize))
  {
    auto recv = m_pCharacteristic->getData();
    memcpy(&m_ReceivedData, recv, m_pCharacteristic->getLength());

    if (m_ReceivedData.Offset == 0)
      m_Steps.resize(m_ReceivedData.TotalSize);

    auto cnt = std::min((uint16_t)(m_ReceivedData.TotalSize - m_ReceivedData.Offset), NumPoints);

    for (uint16_t i = 0; i < cnt; i++)
    {
      m_Steps[m_ReceivedData.Offset + i] = m_ReceivedData.Points[i];
    }

    if (m_ReceivedData.Offset + cnt == m_ReceivedData.TotalSize)
    {
      m_ReceivedDataUpdated = true;
    }
  }
}