/*
This file is part of DominoDrawBluetooth.

DominoDrawBluetooth is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation version 3 or later.

DominoDrawBluetooth is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with DominoDrawBluetooth. If not, see <https://www.gnu.org/licenses/>.
*/

using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using System.Diagnostics;
using System.Xml.Linq;

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
    private Guid _DominoDeviceId;
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

                Debug.WriteLine($"[DeviceDiscovered] Found device {name}:{foundBleDevice.Device.Id}");
                if (name == DEVICE_NAME)
                {
                    FoundRobot(foundBleDevice.Device);
                }
            }
        };

        _bluetoothAdapter.DeviceConnectionLost += (o, args) =>
        {
            Debug.WriteLine($"[DeviceConnectionLost] Device {args.Device.Name}:{args.Device.Id}");
            TryReconnect();
        };
        _bluetoothAdapter.DeviceDisconnected += (o, args) =>
        {
            Debug.WriteLine($"[DeviceDisconnected] Device {args.Device.Name}:{args.Device.Id}");
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
                try
                {
                    byte[] data = ManualCommandData.Write(manualMode);
                    _ManualControlCharacteristic.WriteType = CharacteristicWriteType.WithoutResponse;
                    _ManualControlCharacteristic.WriteAsync(data);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SendManualCommand] Exception writing data: {ex}");
                }
            }
        }
    }

    public async Task SendDrawCommand()
    {
        if (_DrawControlCharacteristic != null && _DrawControlCharacteristic.CanWrite)
        {
            try
            {
                var len = DrawCommandData.DrivePath.Count;

                for (int i = 0; i < len; i += DominoDrawCommandData.MaxPoints)
                {
                    var data = DrawCommandData.Write(i);

                    await _DrawControlCharacteristic.WriteAsync(data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SendDrawCommand] Exception writing data: {ex}");
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
        {
            Debug.WriteLine($"[CancelScan] Cancel scan");
            CancelControl.Cancel();
        }
        else
            Debug.WriteLine($"[CancelScan] CancelControl is null");
    }

    public async Task Disconnect()
    {
        if (!IsConnected)
        {
            Debug.WriteLine($"[Disconnect] Not connected");
            return;
        }

        Debug.WriteLine($"[Disconnect] Call DisconnectDeviceAsync()");
        Debug.WriteLine($"[Disconnect] _DominoDevice={_DominoDevice.Name}:{_DominoDevice.Id}");
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
        {
            Debug.WriteLine("[Scan] Trivial exit, already connected");
            return;
        }

        Debug.WriteLine("[Scan] Enter");
        _Popup = popup;

#if ANDROID
        if (!await PermissionsGrantedAsync())
        {
            await _Owner.DisplayAlert("Permission required", "Application needs location permission", "OK");
            return;
        }
#endif

        if (_DominoDeviceId == Guid.Empty)
        {
            if (!_bluetoothAdapter.IsScanning)
            {
                Debug.WriteLine("[Scan] _DominoDeviceId not set, start scan");
                ScanFilterOptions scanOptions = new ScanFilterOptions();
                scanOptions.DeviceNames = [DEVICE_NAME];
                CancelControl = new CancellationTokenSource();

                await _bluetoothAdapter.StartScanningForDevicesAsync(scanOptions, null, false, CancelControl.Token);
            }
            else
                Debug.WriteLine("[Scan] _DominoDeviceId not set, _bluetoothAdapter.IsScanning already true");
            if (_DominoDevice == null)
            {
                _Popup = null;
                CancelControl = null;
                return;
            }
            // Found robot
            await _bluetoothAdapter.StopScanningForDevicesAsync();
        }
        else
        {
            Debug.WriteLine($"[Scan] _DominoDeviceId already found as {_DominoDeviceId}");
        }
        Debug.WriteLine("[Scan] Call ConnectToServices()");
        if (!await ConnectToServices())
        {
            Debug.WriteLine("[Scan] ConnectToServices failed");
            _DominoDevice = null;
            _DominoService = null;
            _StatusCharacteristic = null;
            _ManualControlCharacteristic = null;
            _DrawControlCharacteristic = null;
            await _Owner.DisplayAlert("Error connecting", "Failed to connect to robot", "OK");
        }
        _Popup = null;
    }

    private async Task<bool> ConnectToServices()
    {
        if (_DominoDeviceId == Guid.Empty)
        {
            Debug.WriteLine($"[ConnectToServices] _DominoDeviceId not set, abort()");
            return false;
        }

        Debug.WriteLine($"[ConnectToServices] Call ConnectToKnownDeviceAsync({_DominoDeviceId})");
        _DominoDevice = await _bluetoothAdapter.ConnectToKnownDeviceAsync(_DominoDeviceId);
        if (_DominoDevice == null)
        {
            Debug.WriteLine($"[ConnectToServices] Failed to connect to robot");
            return false;
        }
        Debug.WriteLine($"[ConnectToServices] Returned from ConnectToKnownDeviceAsync()");
        Debug.WriteLine($"[ConnectToServices] _DominoDevice={_DominoDevice.Name}:{_DominoDevice.Id}");

        Debug.WriteLine($"[ConnectToServices] Call FindService()");
        if (!await FindService())
        {
            Debug.WriteLine($"[ConnectToServices] FindService() failed");
            return false;
        }
        Debug.WriteLine($"[ConnectToServices] Call FindCharacteristics()");
        if (!await FindCharacteristics())
        {
            Debug.WriteLine($"[ConnectToServices] FindCharacteristics() failed");
            return false;
        }

        Debug.WriteLine($"[ConnectToServices] Finished connecting");
        CancelControl = null;
        StatusData.JustConnected = true;
        OnConnected?.Invoke();

        if (_StatusCharacteristic != null && _StatusCharacteristic.CanUpdate)
        {
            // define a callback function
            _StatusCharacteristic.ValueUpdated += (o, args) =>
            {
                try
                {
                    var receivedBytes = args.Characteristic.Value;

                    if (receivedBytes != null)
                        StatusData.Read(receivedBytes);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[_StatusCharacteristic ValueUpdated Callback] Exception reading data: {ex}");
                }
            };
            await _StatusCharacteristic.StartUpdatesAsync();
        }

        return true;
    }

    private void FoundRobot(IDevice device)
    {
        Debug.WriteLine($"[FoundRobot] Found {device.Name}:{device.Id}");
        _Popup.SetMessage("Found Robot, looking for service");
        _DominoDeviceId = device.Id;
        CancelControl.Cancel();
    }

    protected async Task<bool> FindService()
    {
        if (_DominoDevice == null)
        {
            Debug.WriteLine($"[FindService] _DominoDevice is null");
            await _Owner.DisplayAlert("Error connecting", "[FindService] Device is null", "OK");
            return false;
        }
        try
        {
            Debug.WriteLine($"[FindService] Call _DominoDevice.GetServicesAsync()");
            var servicesListReadOnly = await _DominoDevice.GetServicesAsync();

            Debug.WriteLine($"[FindService] Found {servicesListReadOnly.Count} services");
            for (int i = 0; i < servicesListReadOnly.Count; i++)
            {
                if (servicesListReadOnly[i].Id == SERVICE_UUID)
                {
                    Debug.WriteLine($"[FindService] Found domino service {servicesListReadOnly[i].Name}:{servicesListReadOnly[i].Id}");
                    _DominoService = servicesListReadOnly[i];
                    if (_Popup != null)
                        _Popup.SetMessage("Found Service");
                    break;
                }
                else
                    Debug.WriteLine($"[FindService] Found other service {servicesListReadOnly[i].Name}:{servicesListReadOnly[i].Id}");
            }
            return true;
        }
        catch (ObjectDisposedException ex)
        {
            Debug.WriteLine($"[FindService] Caught ObjectDisposedException");
            Debug.WriteLine($"[FindService] {ex}");
        }
        catch (Exception ex)
        {
            await _Owner.DisplayAlert("Error initializing", $"Error finding services. \n{ex}", "OK");
        }
        return false;
    }

    protected async Task<bool> FindCharacteristics()
    {
        if (_DominoService == null)
        {
            Debug.WriteLine($"[FindCharacteristics] _DominoService is null");
            await _Owner.DisplayAlert("Error connecting", "[FindCharacteristics] Device is null", "OK");
            return false;
        }
        try
        {
            Debug.WriteLine($"[FindCharacteristics] Call _DominoService.GetCharacteristicsAsync()");
            var charListReadOnly = await _DominoService.GetCharacteristicsAsync();

            Debug.WriteLine($"[FindCharacteristics] Found {charListReadOnly.Count} characteristics");
            for (int i = 0; i < charListReadOnly.Count; i++)
            {
                if (charListReadOnly[i].Id == STATUS_CHARACTERISTIC_UUID)
                {
                    Debug.WriteLine($"[FindService] Found status characteristic {charListReadOnly[i].Name}:{charListReadOnly[i].Id}");
                    if (_Popup != null)
                        _Popup.SetMessage("Found Status Characteristics");
                    _StatusCharacteristic = charListReadOnly[i];
                }
                else if (charListReadOnly[i].Id == MANUAL_CONTROL_CHARACTERISTIC_UUID)
                {
                    Debug.WriteLine($"[FindService] Found manual characteristic {charListReadOnly[i].Name}:{charListReadOnly[i].Id}");
                    if (_Popup != null)
                        _Popup.SetMessage("Found Manual Control Characteristics");
                    _ManualControlCharacteristic = charListReadOnly[i];
                }
                else if (charListReadOnly[i].Id == DRAW_CONTROL_CHARACTERISTIC_UUID)
                {
                    Debug.WriteLine($"[FindService] Found draw characteristic {charListReadOnly[i].Name}:{charListReadOnly[i].Id}");
                    if (_Popup != null)
                        _Popup.SetMessage("Found Draw Control Characteristics");
                    _DrawControlCharacteristic = charListReadOnly[i];
                }
                else
                    Debug.WriteLine($"[FindService] Found other characteristic {charListReadOnly[i].Name}:{charListReadOnly[i].Id}");
            }
            return true;
        }
        catch (ObjectDisposedException ex)
        {
            Debug.WriteLine($"[FindCharacteristics] Caught ObjectDisposedException");
            Debug.WriteLine($"[FindCharacteristics] {ex}");
        }
        catch (Exception ex)
        {
            await _Owner.DisplayAlert("Error initializing", $"Error finding characteristics. \n{ex}", "OK");
        }
        return false;
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
            Debug.WriteLine($"[HandleDisconnect] Set all to null");
            _DominoDevice = null;
            _DominoService = null;
            _StatusCharacteristic = null;
            _ManualControlCharacteristic = null;
            _DrawControlCharacteristic = null;
            Debug.WriteLine($"[HandleDisconnect] Trigger OnDisconnected");
            OnDisconnected?.Invoke();
        }
        else
            Debug.WriteLine($"[HandleDisconnect] _DominoDevice is already null");
    }

    private async Task TryReconnect()
    {
        if (IsConnected)
        {
            Debug.WriteLine($"[TryReconnect] Set all to null");
            _DominoDevice = null;
            _DominoService = null;
            _StatusCharacteristic = null;
            _ManualControlCharacteristic = null;
            _DrawControlCharacteristic = null;
            Debug.WriteLine($"[TryReconnect] Trigger OnDisconnected");
            OnDisconnected?.Invoke();
        }
        else
            Debug.WriteLine($"[TryReconnect] _DominoDevice is already null");

        Debug.WriteLine($"[TryReconnect] Call ConnectToServices()");
        if (!await ConnectToServices())
        {
            Debug.WriteLine($"[TryReconnect] ConnectToServices() returned false");
            _DominoDevice = null;
            _DominoService = null;
            _StatusCharacteristic = null;
            _ManualControlCharacteristic = null;
            _DrawControlCharacteristic = null;
            await _Owner.DisplayAlert("Error connecting", "Failed to reconnect to robot", "OK");
        }
        else
            Debug.WriteLine($"[TryReconnect] ConnectToServices() returned true");
    }
}
