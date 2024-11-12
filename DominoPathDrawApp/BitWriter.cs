/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/

namespace DominoPathDrawApp;

public static class BitWriter
{
    public static byte Read(this byte[] source, int offset)
    {
        return source[offset];
    }

    public static uint ReadUInt16(this byte[] source, int offset)
    {
        return BitConverter.ToUInt16(source, offset);
    }

    public static uint ReadUInt32(this byte[] source, int offset)
    {
        return BitConverter.ToUInt32(source, offset);
    }

    public static void Write(this byte[] buffer, int offset, byte value)
    {
        buffer[offset] = (byte)value;
    }

    public static void Write(this byte[] buffer, int offset, short value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
    }

    public static void Write(this byte[] buffer, int offset, UInt16 value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
    }

    public static void Write(this byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 0x08);
        buffer[offset + 2] = (byte)(value >> 0x10);
        buffer[offset + 3] = (byte)(value >> 0x18);
    }

    public static void Write(this byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 0x08);
        buffer[offset + 2] = (byte)(value >> 0x10);
        buffer[offset + 3] = (byte)(value >> 0x18);
    }
}
