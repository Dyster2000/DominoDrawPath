/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/

namespace DominoPathDrawApp;

public class MessageBuffer
{
    protected int InitialSize { get; private set; }
    protected byte[] Buffer;
    public int Length { get; private set; }
    public int Size { get { return Buffer.Length; } }

    public MessageBuffer(int bufferSize)
    {
        InitialSize = bufferSize;
        Buffer = new byte[bufferSize];
    }

    public MessageBuffer(byte[] data)
    {
        InitialSize = data.Length;
        Buffer = data;
    }

    public byte[] GetData()
    {
        return Buffer;
    }

    public bool SetSize(int bufferSize)
    {
        if (bufferSize > Size)
        {
            Buffer = new byte[bufferSize];
            return true;
        }
        return false;
    }

    public bool ResetSize()
    {
        if (Size != InitialSize)
        {
            Buffer = new byte[InitialSize];
            return true;
        }
        return false;
    }

    public void Clear()
    {
        Clear(0, Size);
    }
    public void Clear(int index, int length)
    {
        Array.Clear(Buffer, index, length);
    }

    public int Write(int offset, bool value)
    {
        Buffer[offset] = value ? (byte)1 : (byte)0;
        return offset + 1;
    }
    public int Write(int offset, byte value)
    {
        Buffer[offset] = value;
        return offset + 1;
    }
    public int Write(int offset, SByte value)
    {
        Buffer[offset] = (byte)value;
        return offset + 1;
    }
    public int Write(int offset, UInt16 value)
    {
        Buffer.Write(offset, value);
        return offset + 2;
    }
    public int Write(int offset, uint value)
    {
        Buffer.Write(offset, value);
        return offset + 4;
    }

    public byte ReadByte(ref int offset)
    {
        var val = Buffer.Read(offset);
        offset += 1;
        return val;
    }

    public bool ReadBool(ref int offset)
    {
        var val = Buffer.Read(offset);
        offset += 1;
        return val != 0;
    }

    public uint ReadUInt16(ref int offset)
    {
        var val = Buffer.ReadUInt16(offset);
        offset += 2;
        return val;
    }

    public uint ReadUInt32(ref int offset)
    {
        var val = Buffer.ReadUInt32(offset);
        offset += 4;
        return val;
    }
}