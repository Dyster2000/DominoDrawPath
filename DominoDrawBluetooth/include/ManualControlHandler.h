/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/
#include <BLEDevice.h>
#include <BLEServer.h>

class DominoBleServer;
struct DominoData;

#pragma pack(1)

struct ManualCommandData
{
  uint8_t Moving{false};
  uint8_t Dispensing{true};
  uint8_t StopOnEmpty{true};
  uint8_t ManualMode{true}; // Manual vs Draw control
  int8_t Direction{0};
};

#pragma pack()

class ManualControlHandler : public BLECharacteristicCallbacks
{
public:
  ManualControlHandler(DominoData &data, DominoBleServer &server, BLEService &service);
  virtual ~ManualControlHandler() = default;

  void Loop(uint32_t deltaTime);

private:
  virtual void onWrite(BLECharacteristic *pCharacteristic, esp_ble_gatts_cb_param_t *param);

private:
  const char *CHARACTERISTIC_UUID = "874b19c2-4bfa-4453-83b4-e0d3a28317fd";

  DominoData &m_Data;
  DominoBleServer &m_Server;
  BLECharacteristic *m_pCharacteristic;

  ManualCommandData m_ReceivedData;
  bool m_ReceivedDataUpdated;
};