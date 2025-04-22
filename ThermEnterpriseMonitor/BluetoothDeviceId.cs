namespace ThermEnterpriseMonitor;

public readonly struct BluetoothDeviceId
{
    public string Value { get; }

    public BluetoothDeviceId(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(BluetoothDeviceId id)
    {
        return id.Value;
    }

    public static explicit operator BluetoothDeviceId(string value)
    {
        return new(value);
    }
}