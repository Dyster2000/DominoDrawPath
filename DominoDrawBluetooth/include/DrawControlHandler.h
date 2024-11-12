/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/
#include <BLEDevice.h>
#include <BLEServer.h>
#include <list>

class DominoBleServer;
struct DominoData;

#pragma pack(1)

constexpr uint16_t NumPoints = 8;

struct DrawPathStep
{
  uint16_t DistanceMM{0};
  uint16_t Angle{0};
};

struct DrawCommandData
{
  uint16_t Offset{0};
  uint16_t TotalSize{0};
  DrawPathStep Points[NumPoints];
};

#pragma pack()

class DrawControlHandler : public BLECharacteristicCallbacks
{
public:
  DrawControlHandler(DominoData &data, DominoBleServer &server, BLEService &service);
  virtual ~DrawControlHandler() = default;

  void Loop(uint32_t deltaTime);

private:
  virtual void onWrite(BLECharacteristic *pCharacteristic, esp_ble_gatts_cb_param_t *param);

  void SetNextStep();

private:
  const char *CHARACTERISTIC_UUID = "56d0d406-5ae9-4e66-8ff7-bd43c12e6263";
  static constexpr uint16_t MinSize = 6;
  static constexpr uint16_t MaxSize = 20;
  static constexpr float MicrosPerTurnDegree = 1.0/5000000; // 90 degrees over 5 seconds at max turn rate

  DominoData &m_Data;
  DominoBleServer &m_Server;
  BLECharacteristic *m_pCharacteristic;

  DrawCommandData m_ReceivedData;
  bool m_ReceivedDataUpdated;
  std::vector<DrawPathStep> m_Steps;

  int m_CurrentIndex;
  bool m_PathActive;
  uint32_t m_DistanceForNextStep;
  float m_CurrentAngle;
  float m_TargetAngle;
  float m_LastDiff;
  uint32_t m_StepElapsedTimeUS;
};