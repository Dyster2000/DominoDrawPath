﻿/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/

namespace DominoPathDrawApp;

public delegate void Notify();

public class DominoStatusData
{
    static readonly int DataSize = 10;

    public bool Moving { get; set; }
    public bool Dispensing { get; set; }
    public bool StopOnEmpty { get; set; }
    public bool IsEmpty { get; set; }
    public bool ManualMode { get; set; }
    public byte Direction { get; set; }
    public uint DistanceTraveled { get; set; }

    public bool JustConnected { get; set; }

    public event Notify ODataChanged;
    //public event Notify OnMovingChanged;
    //public event Notify OnDispensingChanged;
    //public event Notify OnStopOnEmptyChanged;
    //public event Notify OnIsEmptyChanged;
    //public event Notify OnManualModeChanged;
    //public event Notify OnDirectionChanged;
    //public event Notify OnDistanceTraveledChanged;

    public DominoStatusData()
    {
        JustConnected = false;
    }

    public void Read(byte[] data)
    {
        if (data.Length == DataSize)
        {
            MessageBuffer buffer = new MessageBuffer(data);
            int offset = 0;

            var moving = buffer.ReadBool(ref offset);
            var dispensing = buffer.ReadBool(ref offset);
            var stopOnEmpty = buffer.ReadBool(ref offset);
            var isEmpty = buffer.ReadBool(ref offset);
            var manualMode = buffer.ReadBool(ref offset);
            var direction = buffer.ReadByte(ref offset);
            var distanceTraveled = buffer.ReadUInt32(ref offset);

            //var movingChanged = Moving != moving || JustConnected;
            //var dispensingChanged = Dispensing != dispensing || JustConnected;
            //var stopOnEmptyChanged = StopOnEmpty != stopOnEmpty || JustConnected;
            //var isEmptyChanged = IsEmpty != isEmpty || JustConnected;
            //var manualModeChanged = ManualMode != manualMode || JustConnected;
            //var directionChanged = Direction != direction || JustConnected;
            //var distanceTraveledChanged = DistanceTraveled != distanceTraveled || JustConnected;

            var dataChanged = JustConnected;
            dataChanged = dataChanged || Moving != moving;
            dataChanged = dataChanged || Dispensing != dispensing;
            dataChanged = dataChanged || StopOnEmpty != stopOnEmpty;
            dataChanged = dataChanged || IsEmpty != isEmpty;
            dataChanged = dataChanged || ManualMode != manualMode;
            dataChanged = dataChanged || Direction != direction;
            dataChanged = dataChanged || DistanceTraveled != distanceTraveled;

            Moving = moving;
            Dispensing = dispensing;
            StopOnEmpty = stopOnEmpty;
            IsEmpty = isEmpty;
            ManualMode = manualMode;
            Direction = direction;
            DistanceTraveled = distanceTraveled;

            if (dataChanged)
                ODataChanged?.Invoke();
            //if (movingChanged)
            //    OnMovingChanged?.Invoke();
            //if (dispensingChanged)
            //    OnDispensingChanged?.Invoke();
            //if (stopOnEmptyChanged)
            //    OnStopOnEmptyChanged?.Invoke();
            //if (isEmptyChanged)
            //    OnIsEmptyChanged?.Invoke();
            //if (manualModeChanged)
            //    OnManualModeChanged?.Invoke();
            //if (directionChanged)
            //    OnDirectionChanged?.Invoke();
            //if (distanceTraveledChanged)
            //    OnDistanceTraveledChanged?.Invoke();

            JustConnected = false;
        }
    }
}