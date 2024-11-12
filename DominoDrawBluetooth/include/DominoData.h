/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/
#pragma pack(1)

struct DominoData
{
  uint8_t Moving{false};
  uint8_t Dispensing{true};
  uint8_t StopOnEmpty{true};
  uint8_t IsEmpty{false};
  uint8_t ManualMode{true}; // Manual vs Draw control
  int8_t Direction{0};
  uint32_t DistanceTraveledMM{0};
};

#pragma pack()
