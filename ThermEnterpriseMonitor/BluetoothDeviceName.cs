namespace ThermEnterpriseMonitor;

public readonly struct BluetoothDeviceName
{
    public string Value { get; }

    public BluetoothDeviceName(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(BluetoothDeviceName id)
    {
        return id.Value;
    }

    public static explicit operator BluetoothDeviceName(string value)
    {
        return new(value);
    }
}