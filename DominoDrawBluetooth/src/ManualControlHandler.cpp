/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/
#include "ManualControlHandler.h"
#include "DominoBleServer.h"
#include "DominoData.h"
#include <Arduino.h>
#include <BLEUtils.h>
#include <BLE2902.h>

ManualControlHandler::ManualControlHandler(DominoData &data, DominoBleServer &server, BLEService &service)
  : m_Data(data)
  , m_Server(server)
  , m_ReceivedDataUpdated(false)
{
  // Create a BLE Characteristic
  m_pCharacteristic = service.createCharacteristic(
      CHARACTERISTIC_UUID,
      BLECharacteristic::PROPERTY_READ | BLECharacteristic::PROPERTY_WRITE_NR);

  // Create a BLE Descriptor
  m_pCharacteristic->addDescriptor(new BLE2902());

  m_pCharacteristic->setCallbacks(this);
}

void ManualControlHandler::Loop(uint32_t deltaTime)
{
  if (m_ReceivedDataUpdated)
  {
    m_ReceivedDataUpdated = false;
    m_Data.StopOnEmpty = m_ReceivedData.StopOnEmpty;
    m_Data.Dispensing = m_ReceivedData.Dispensing;
    m_Data.ManualMode = m_ReceivedData.ManualMode;
    
    // Only take the direction if in manual mode. Draw mode will control direction with the path list
    if (m_Data.ManualMode)
      m_Data.Direction = m_ReceivedData.Direction;

    // Check if not moving and requesting to move
    if (m_ReceivedData.Moving && !m_Data.Moving)
    {
      if (!m_Data.IsEmpty || !m_Data.StopOnEmpty)
      {
        m_Data.Moving = true;
      }
    }
    else if (!m_ReceivedData.Moving && m_Data.Moving)
    {
      m_Data.Moving = false;
    }
  }
}

void ManualControlHandler::onWrite(BLECharacteristic *pCharacteristic, esp_ble_gatts_cb_param_t *param)
{
  if (m_pCharacteristic->getLength() == sizeof(ManualCommandData))
  {
    auto recv = m_pCharacteristic->getData();
    memcpy(&m_ReceivedData, recv, sizeof(ManualCommandData));
    m_ReceivedDataUpdated = true;
  }
}