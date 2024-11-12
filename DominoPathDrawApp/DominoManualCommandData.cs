/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/

namespace DominoPathDrawApp;

public class DominoManualCommandData
{
    static readonly int DataSize = 5;

    public bool Moving { get; set; } = false;
    public bool Dispensing { get; set; } = true;
    public bool StopOnEmpty { get; set; } = true;
    public byte Direction { get; set; } = 0;

    MessageBuffer buffer;

    public DominoManualCommandData()
    {
        buffer = new MessageBuffer(DataSize);
    }

    public byte[] Write(bool manualMode)
    {
        int offset = 0;

        offset = buffer.Write(offset, Moving);
        offset = buffer.Write(offset, Dispensing);
        offset = buffer.Write(offset, StopOnEmpty);
        offset = buffer.Write(offset, manualMode);
        offset = buffer.Write(offset, Direction);

        return buffer.GetData();
    }

    public void UpdateFromStatus(DominoStatusData statusData)
    {
        Moving = statusData.Moving;
        Dispensing = statusData.Dispensing;
        StopOnEmpty = statusData.StopOnEmpty;
        Direction = statusData.Direction;
    }
}
