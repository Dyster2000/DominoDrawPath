/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/

using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;

namespace DominoPathDrawApp;

public class BleHandler
{
    public bool IsConnected { get { return _DominoDevice != null; } }

    public DominoManualCommandData ManualCommandData { get; private set; }
    public DominoDrawCommandData DrawCommandData { get; private set; }

    public DominoStatusData StatusData { get; private set; }

    public event Notify OnDisconnected;
    public event Notify OnConnected;

    private readonly string DEVICE_NAME = "HackPackDomino";
    private readonly Guid SERVICE_UUID = new Guid("faa94de0-cd7c-43fa-b71d-40324ff9ab2b");
    private readonly Guid STATUS_CHARACTERISTIC_UUID = new Guid("b43a1a69-5dc4-4573-b47c-53e31ca661f2");
    private readonly Guid MANUAL_CONTROL_CHARACTERISTIC_UUID = new Guid("874b19c2-4bfa-4453-83b4-e0d3a28317fd");
    private readonly Guid DRAW_CONTROL_CHARACTERISTIC_UUID = new Guid("56d0d406-5ae9-4e66-8ff7-bd43c12e6263");

    private ScanPopup _Popup;
    private Page _Owner;
    private CancellationTokenSource CancelControl;
    private readonly IAdapter _bluetoothAdapter;  // Class for the Bluetooth adapter
    private IDevice? _DominoDevice;
    private IService? _DominoService;
    private ICharacteristic? _StatusCharacteristic;
    private ICharacteristic? _ManualControlCharacteristic;
    private ICharacteristic? _DrawControlCharacteristic;

    private readonly object WriteSync = new object();

    public BleHandler(Page owner)
    {
        ManualCommandData = new DominoManualCommandData();
        DrawCommandData = new DominoDrawCommandData();
        StatusData = new DominoStatusData();

        _Owner = owner;

        _bluetoothAdapter = CrossBluetoothLE.Current.Adapter;               // Point _bluetoothAdapter to the current adapter on the phone
        _bluetoothAdapter.DeviceDiscovered += (sender, foundBleDevice) =>   // When a BLE Device is found, run the small function below to add it to our list
        {
            if (foundBleDevice.Device != null && foundBleDevice.Device.Name != null)
            {
                var name = foundBleDevice.Device.Name;

                if (name == DEVICE_NAME)
                {
                    FoundRobot(foundBleDevice.Device);
                }
            }
        };

        _bluetoothAdapter.DeviceConnectionLost += (o, args) =>
        {
            HandleDisconnect();
        };
        _bluetoothAdapter.DeviceDisconnected += (o, args) =>
        {
            HandleDisconnect();
        };
    }

    public async Task<bool> CheckBluetoothStatus()
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

    public void SendManualCommand(bool manualMode)
    {
        if (_ManualControlCharacteristic != null && _ManualControlCharacteristic.CanWrite)
        {
            lock (WriteSync)
            {
                byte[] data = ManualCommandData.Write(manualMode);
                _ManualControlCharacteristic.WriteType = CharacteristicWriteType.WithoutResponse;
                _ManualControlCharacteristic.WriteAsync(data);
            }
        }
    }

    public async Task SendDrawCommand()
    {
        if (_DrawControlCharacteristic != null && _DrawControlCharacteristic.CanWrite)
        {
            var len = DrawCommandData.DrivePath.Count;

            for (int i = 0; i < len; i += DominoDrawCommandData.MaxPoints)
            {
                var data = DrawCommandData.Write(i);

                await _DrawControlCharacteristic.WriteAsync(data);
            }
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
            // logger.LogError(ex);
            return false;
        }
    }

    public void CancelScan()
    {
        if (CancelControl != null)
            CancelControl.Cancel();
    }

    public async Task Disconnect()
    {
        if (!IsConnected)
            return;

        await _bluetoothAdapter.DisconnectDeviceAsync(_DominoDevice);
        _DominoDevice = null;
        _DominoService = null;
        _StatusCharacteristic = null;
        _ManualControlCharacteristic = null;
        _DrawControlCharacteristic = null;
    }

    public async Task Scan(ScanPopup popup)           // Function that is called when the scanButton is pressed
    {
        if (IsConnected)
            return;

        _Popup = popup;

#if ANDROID
        if (!await PermissionsGrantedAsync())
        {
            await _Owner.DisplayAlert("Permission required", "Application needs location permission", "OK");
            return;
        }
#endif

        if ((_DominoDevice == null) && (!_bluetoothAdapter.IsScanning))
        {
            ScanFilterOptions scanOptions = new ScanFilterOptions();
            scanOptions.DeviceNames = [DEVICE_NAME];
            CancelControl = new CancellationTokenSource();

            await _bluetoothAdapter.StartScanningForDevicesAsync(scanOptions, null, false, CancelControl.Token);
        }
        if (_DominoDevice == null)
        {
            CancelControl = null;
            return;
        }
        // Found robot
        await _bluetoothAdapter.StopScanningForDevicesAsync();
        await _bluetoothAdapter.ConnectToDeviceAsync(_DominoDevice);
        await FindService();
        if (_DominoService != null)
        {
            await FindCharacteristics();
        }

        CancelControl = null;
        StatusData.JustConnected = true;
        OnConnected?.Invoke();

        if (_StatusCharacteristic != null && _StatusCharacteristic.CanUpdate)
        {
            // define a callback function
            _StatusCharacteristic.ValueUpdated += (o, args) =>
            {
                var receivedBytes = args.Characteristic.Value;

                if (receivedBytes != null)
                    StatusData.Read(receivedBytes);
            };
            await _StatusCharacteristic.StartUpdatesAsync();
        }
    }

    private void FoundRobot(IDevice device)
    {
        _Popup.SetMessage("Found Robot, looking for service");
        _DominoDevice = device;
        CancelControl.Cancel();
    }

    protected async Task FindService()
    {
        if (_DominoDevice == null)
        {
            throw new ArgumentNullException(nameof(_DominoDevice), "Parameter cannot be null");
        }
        try
        {
            var servicesListReadOnly = await _DominoDevice.GetServicesAsync();

            for (int i = 0; i < servicesListReadOnly.Count; i++)
            {
                if (servicesListReadOnly[i].Id == SERVICE_UUID)
                {
                    _DominoService = servicesListReadOnly[i];
                    _Popup.SetMessage("Found Service");
                    break;
                }
            }
        }
        catch
        {
            await _Owner.DisplayAlert("Error initializing", $"Error initializing UART GATT service.", "OK");
        }
    }

    protected async Task FindCharacteristics()
    {
        if (_DominoService == null)
        {
            throw new ArgumentNullException(nameof(_DominoDevice), "Parameter cannot be null");
        }
        try
        {
            var charListReadOnly = await _DominoService.GetCharacteristicsAsync();

            for (int i = 0; i < charListReadOnly.Count; i++)
            {
                if (charListReadOnly[i].Id == STATUS_CHARACTERISTIC_UUID)
                {
                    _Popup.SetMessage("Found Status Characteristics");
                    _StatusCharacteristic = charListReadOnly[i];
                }
                else if (charListReadOnly[i].Id == MANUAL_CONTROL_CHARACTERISTIC_UUID)
                {
                    _Popup.SetMessage("Found Manual Control Characteristics");
                    _ManualControlCharacteristic = charListReadOnly[i];
                }
                else if (charListReadOnly[i].Id == DRAW_CONTROL_CHARACTERISTIC_UUID)
                {
                    _Popup.SetMessage("Found Draw Control Characteristics");
                    _DrawControlCharacteristic = charListReadOnly[i];
                }
            }
        }
        catch
        {
            await _Owner.DisplayAlert("Error initializing", $"Error initializing UART GATT service.", "OK");
        }
    }

    private async Task<bool> PermissionsGrantedAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        return status == PermissionStatus.Granted;
    }

    private void HandleDisconnect()
    {
        if (IsConnected)
        {
            _DominoDevice = null;
            _DominoService = null;
            _StatusCharacteristic = null;
            _ManualControlCharacteristic = null;
            _DrawControlCharacteristic = null;
            OnDisconnected?.Invoke();
        }
    }
}
