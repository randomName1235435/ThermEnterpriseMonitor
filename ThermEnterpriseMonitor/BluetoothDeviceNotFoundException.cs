namespace ThermEnterpriseMonitor;

internal class BluetoothDeviceNotFoundException : Exception
{
    public BluetoothDeviceNotFoundException(string deviceName)
    {
        this.DeviceName = deviceName;
    }

    public string DeviceName { get; }
}