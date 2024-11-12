/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/
#include <BLEServer.h>

class BLEServer;
class BLEService;
class StatusHandler;
class ManualControlHandler;
class DrawControlHandler;
struct DominoData;

class DominoBleServer : public BLEServerCallbacks
{
public:
  DominoBleServer(DominoData &data);
  virtual ~DominoBleServer() = default;

  void Init();

  void Loop(uint32_t deltaTime);

  bool IsConnected();

private:
  void CheckConnection();

  void onConnect(BLEServer *pServer) override;
  void onDisconnect(BLEServer *pServer) override;

private:
  const char *SERVICE_UUID = "faa94de0-cd7c-43fa-b71d-40324ff9ab2b";

  DominoData &m_Data;
  BLEServer *m_pServer;
  BLEService *m_pService;
  StatusHandler *m_pStatus;
  ManualControlHandler *m_pManualControl;
  DrawControlHandler *m_pDrawControl;

  bool m_DeviceConnected = false;
  bool m_OldDeviceConnected = false;
};
