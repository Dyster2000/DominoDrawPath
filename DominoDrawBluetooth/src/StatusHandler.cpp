/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/
#include "StatusHandler.h"
#include "DominoBleServer.h"
#include "DominoData.h"
#include <Arduino.h>
#include <BLEUtils.h>
#include <BLE2902.h>

StatusHandler::StatusHandler(DominoData &data, DominoBleServer &server, BLEService &service)
  : m_Data(data)
  , m_Server(server)
  , m_NextStatusTime(0)
{
  // Create a BLE Characteristic
  Serial.println("[DominoBleServer] Create characteristic");
  m_pCharacteristic = service.createCharacteristic(
      CHARACTERISTIC_UUID,
      BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_NOTIFY | BLECharacteristic::PROPERTY_INDICATE);

  // Create a BLE Descriptor
  m_pCharacteristic->addDescriptor(new BLE2902());
}

void StatusHandler::Loop(uint32_t deltaTime)
{
  if (m_Server.IsConnected())
  {
    uint32_t currentTime = millis();

    if (currentTime > m_NextStatusTime)
    {
      m_pCharacteristic->setValue((uint8_t *)&m_Data, sizeof(DominoData));
      m_pCharacteristic->notify();

      m_NextStatusTime = millis() + UPDATE_TIME;
    }
  }
}