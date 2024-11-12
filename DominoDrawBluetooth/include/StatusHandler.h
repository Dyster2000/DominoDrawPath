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

class StatusHandler
{
public:
  StatusHandler(DominoData &data, DominoBleServer &server, BLEService &service);

  void Loop(uint32_t deltaTime);

private:
  const char *CHARACTERISTIC_UUID = "b43a1a69-5dc4-4573-b47c-53e31ca661f2";
  static constexpr uint32_t UPDATE_TIME = 1000;

  DominoData &m_Data;
  DominoBleServer &m_Server;
  BLECharacteristic *m_pCharacteristic;

  uint32_t m_NextStatusTime;
};