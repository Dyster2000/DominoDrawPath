/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/

namespace DominoPathDrawApp;

public partial class CommonDriveView : ContentView
{
    private BleHandler Ble { get; set; }
    private bool ManualMode { get; set; }
    private bool MoveNotAllowed { get; set; } = false;

    public CommonDriveView()
    {
        InitializeComponent();
    }

    public void Init(BleHandler ble, bool manualMode)
    {
        Ble = ble;
        ManualMode = manualMode;

        Ble.OnConnected += Ble_OnConnected;
        Ble.OnDisconnected += Ble_OnDisconnected;
        Ble.StatusData.OnDataChanged += StatusData_OnMovingChanged;
        Ble.StatusData.OnDataChanged += StatusData_OnDispensingChanged;
        Ble.StatusData.OnDataChanged += StatusData_OnStopOnEmptyChanged;
    }

    public void EnableMoving(bool enable)
    {
        MoveNotAllowed = !enable;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MoveButton.IsEnabled = enable;
        });
    }

    private void Ble_OnConnected()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsEnabled = true;
        });
    }

    private void Ble_OnDisconnected()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsEnabled = false;
        });
    }

    private void StatusData_OnMovingChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // If manual mode enables moving, then draw mode can't and vice versa
            if (MoveNotAllowed)
                MoveButton.IsEnabled = false;
            else
                MoveButton.IsEnabled = !Ble.StatusData.Moving || ManualMode == Ble.StatusData.ManualMode;

            if (Ble.StatusData.Moving)
            {
                MoveButton.Text = "Moving";
                MoveButton.BackgroundColor = Colors.Green;
            }
            else
            {
                MoveButton.Text = "Stopped";
                MoveButton.BackgroundColor = Colors.Red;
            }
        });
    }

    private void StatusData_OnDispensingChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Ble.StatusData.Dispensing)
            {
                DispenseButton.Text = "Dispense Dominoes - On";
                DispenseButton.BackgroundColor = Colors.Green;
            }
            else
            {
                DispenseButton.Text = "Dispense Dominoes - Off";
                DispenseButton.BackgroundColor = Colors.Red;
            }
        });
    }

    private void StatusData_OnStopOnEmptyChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StopOnEmpty.IsChecked = Ble.StatusData.StopOnEmpty;
        });
    }

    private async void MoveButton_Clicked(object sender, EventArgs e)
    {
        Ble.ManualCommandData.UpdateFromStatus(Ble.StatusData);
        Ble.ManualCommandData.Moving = !Ble.StatusData.Moving; // Invert from current

        Ble.SendManualCommand(ManualMode);
    }

    private async void DispenseButton_Clicked(object sender, EventArgs e)
    {
        Ble.ManualCommandData.UpdateFromStatus(Ble.StatusData);
        Ble.ManualCommandData.Dispensing = !Ble.StatusData.Dispensing; // Invert from current

        Ble.SendManualCommand(ManualMode);
    }

    private async void StopOnEmpty_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        Ble.ManualCommandData.UpdateFromStatus(Ble.StatusData);
        Ble.ManualCommandData.StopOnEmpty = StopOnEmpty.IsChecked;

        Ble.SendManualCommand(ManualMode);
    }
}