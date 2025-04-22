namespace ThermEnterpriseMonitor;

public class SensorReading
{
    public DateTime Timestamp { get; set; }
    public float TemperatureCelsius { get; set; }
    public int HumidityPercent { get; set; }
    public bool LastElement { get; set; }
}