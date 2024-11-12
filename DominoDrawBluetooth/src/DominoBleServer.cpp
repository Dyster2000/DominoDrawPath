/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/
#include "DominoBleServer.h"
#include "StatusHandler.h"
#include "ManualControlHandler.h"
#include "DrawControlHandler.h"
#include "DominoData.h"
#include <Arduino.h>
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>

DominoBleServer::DominoBleServer(DominoData &data)
  : m_Data(data)
{
}

void DominoBleServer::Init()
{
  // Create the BLE Device
  BLEDevice::init("HackPackDomino");

  // Create the BLE Server
  Serial.println("[DominoBleServer] Start service");
  m_pServer = BLEDevice::createServer();
  m_pServer->setCallbacks(this);

  // Create the BLE Service
  Serial.println("[DominoBleServer] Create service");
  BLEService *pService = m_pServer->createService(SERVICE_UUID);

  // Create the BLE Characteristics
  m_pStatus = new StatusHandler(m_Data, *this, *pService);
  m_pManualControl = new ManualControlHandler(m_Data, *this, *pService);
  m_pDrawControl = new DrawControlHandler(m_Data, *this, *pService);

  // Start the service
  Serial.println("[DominoBleServer] Start service");
  pService->start();

  // Start advertising
  Serial.println("[DominoBleServer] Start advertising");
  BLEAdvertising *pAdvertising = BLEDevice::getAdvertising();
  pAdvertising->addServiceUUID(SERVICE_UUID);
  pAdvertising->setScanResponse(false);
  pAdvertising->setMinPreferred(0x0); // set value to 0x00 to not advertise this parameter
  BLEDevice::startAdvertising();
}

void DominoBleServer::Loop(uint32_t deltaTime)
{
  CheckConnection();

  m_pManualControl->Loop(deltaTime);
  m_pDrawControl->Loop(deltaTime);
  m_pStatus->Loop(deltaTime);
}

void DominoBleServer::CheckConnection()
{
  // disconnecting
  if (!m_DeviceConnected && m_OldDeviceConnected)
  {
    delay(500);                    // give the bluetooth stack the chance to get things ready
    m_pServer->startAdvertising(); // restart advertising
    Serial.println("start advertising");
    m_OldDeviceConnected = m_DeviceConnected;
  }
  // connecting
  if (m_DeviceConnected && !m_OldDeviceConnected)
  {
    // do stuff here on connecting
    m_OldDeviceConnected = m_DeviceConnected;
  }
}

bool DominoBleServer::IsConnected()
{
  return m_DeviceConnected;
}

void DominoBleServer::onConnect(BLEServer *pServer)
{
  m_DeviceConnected = true;
  Serial.println("Device connected...");
};

void DominoBleServer::onDisconnect(BLEServer *pServer)
{
  m_DeviceConnected = false;
  m_Data.Moving = false;
  Serial.println("Device disconnected...");
}
