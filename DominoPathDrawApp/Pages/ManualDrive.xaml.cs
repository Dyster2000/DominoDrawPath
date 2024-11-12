/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/

namespace DominoPathDrawApp;

public partial class ManualDrive : ContentPage
{
    private BleHandler Ble { get; set; }

    public ManualDrive()
    {
        InitializeComponent();
    }

    public void Init(BleHandler ble)
    {
        Ble = ble;

        BleConnect.Init(this, Ble);
        DriveControls.Init(Ble, true);
    }

    private async void RangePointer_ValueChanged(object sender, Syncfusion.Maui.Gauges.ValueChangedEventArgs e)
    {
        var dir = ((int)e.Value - 50) * 90 / 40; // Convert 10 to 90 range -> -90 to  +90

        Ble.ManualCommandData.UpdateFromStatus(Ble.StatusData);
        Ble.ManualCommandData.Direction = (byte)dir;

        Ble.SendManualCommand(true);
    }
}