/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/

using CommunityToolkit.Maui.Views;

namespace DominoPathDrawApp;

public partial class BleConnectView : ContentView
{
    private Page Owner { get; set; }
    private BleHandler Ble { get; set; }

    public BleConnectView()
    {
        InitializeComponent();
    }

    public void Init(Page owner, BleHandler ble)
    {
        Owner = owner;
        Ble = ble;

        Ble.OnConnected += Ble_OnConnected;
        Ble.OnDisconnected += Ble_OnDisconnected;
    }

    private async void ConnectButton_Clicked(object sender, EventArgs e)
    {
        if (!Ble.IsConnected)
        {
            if (!await Ble.CheckBluetoothStatus())
            {
                if (!await Ble.RequestBluetoothAccess())
                    return;
            }
            var popup = new ScanPopup(Ble);

            Owner.ShowPopup(popup);
            await Ble.Scan(popup);
            popup.Close();
        }
        else
        {
            Ble.Disconnect();
        }
    }

    private void Ble_OnConnected()
    {
        Console.WriteLine("[BleConnectView::Ble_OnConnected] Handle connect");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConnectLabel.Text = "Connected";
            ConnectBar.BackgroundColor = Colors.Green;
            ConnectButton.Text = "Disconnect";
        });
    }

    private void Ble_OnDisconnected()
    {
        Console.WriteLine("[BleConnectView::Ble_OnDisconnected] Handle disconnect");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ConnectLabel.Text = "Not Connected";
            ConnectBar.BackgroundColor = Colors.Red;
            ConnectButton.Text = "Connect";
        });
    }
}
