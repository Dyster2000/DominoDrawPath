/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/

namespace DominoPathDrawApp;

public partial class MainTabPage : TabbedPage
{
    private BleHandler _Ble;

    public MainTabPage()
    {
        InitializeComponent();

        _Ble = new BleHandler(this);
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        Manual.Init(_Ble);
        Draw.Init(_Ble);

        if (!await CheckBluetoothStatus())
            await RequestBluetoothAccess();
    }

    private async Task<bool> CheckBluetoothStatus()
    {
        try
        {
            var requestStatus = await new BluetoothPermissions().CheckStatusAsync();
            return requestStatus == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public async Task<bool> RequestBluetoothAccess()
    {
        try
        {
            var requestStatus = await new BluetoothPermissions().RequestAsync();
            return requestStatus == PermissionStatus.Granted;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}
