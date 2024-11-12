/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/

namespace DominoPathDrawApp;

public struct PathStep
{
    public UInt16 DistanceMM { get; set; }
    public UInt16 Angle { get; set; }

    public PathStep(UInt16 distance, UInt16 angle)
    {
        DistanceMM = distance;
        Angle = angle;
    }

    public override string ToString()
    {
        return $"({DistanceMM}, {Angle})";
    }
}

public class DominoDrawCommandData
{
    static readonly int HeaderSize = 4;
    public static readonly int MaxPoints = 4;

    public List<PathStep> DrivePath { get; set; }

    MessageBuffer Buffer;

    public DominoDrawCommandData()
    {
    }

    public byte[] Write(int startIndex)
    {
        int cnt = Math.Min(DrivePath.Count - startIndex, MaxPoints);
        int offset = 0;

        //Console.WriteLine($"[DominoDrawCommandData::Write] Create MessageBuffer size {HeaderSize + cnt * 4}");
        Buffer = new MessageBuffer(HeaderSize + cnt * 4);

        //Console.WriteLine($"[DominoDrawCommandData::Write] Encode data {startIndex} to {startIndex + cnt}, DrivePath len={DrivePath.Count}");
        //Console.WriteLine($"[DominoDrawCommandData::Write] Add Offset: offset={offset}");
        offset = Buffer.Write(offset, (UInt16)startIndex);
        //Console.WriteLine($"[DominoDrawCommandData::Write] Add TotalSize: offset={offset}");
        offset = Buffer.Write(offset, (UInt16)DrivePath.Count);
        for (int i = 0; i < cnt; i++)
        {
            //Console.WriteLine($"[DominoDrawCommandData::Write] Add X({i}): offset={offset}");
            offset = Buffer.Write(offset, DrivePath[startIndex + i].DistanceMM);
            //Console.WriteLine($"[DominoDrawCommandData::Write] Add Y({i}): offset={offset}");
            offset = Buffer.Write(offset, DrivePath[startIndex + i].Angle);
        }
        //Console.WriteLine($"[DominoDrawCommandData::Write] End offset={offset}");

        return Buffer.GetData();
    }
}
