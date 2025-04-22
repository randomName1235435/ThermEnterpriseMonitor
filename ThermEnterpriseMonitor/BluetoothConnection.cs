using InTheHand.Bluetooth;

namespace ThermEnterpriseMonitor;

public class BluetoothConnection
{
    private const string sensorServiceUuid = "00010203-0405-0607-0809-0a0b0c0d1910";

    private const string feedCharacteristicUuid = "00010203-0405-0607-0809-0a0b0c0d2b10";

    private const string historyCharacteristicUuid = "00010203-0405-0607-0809-0a0b0c0d2b11";

    private static bool allFetched;

    private readonly byte[]
        commandBadderyEmpty = { 0xa2, 0x00, 0x00, 0x00, 0x00, 0x2a }; // badderie auf leer setzen? :D:D 

    private readonly byte[] commandReadLastDay = { 0xa7, 0x00, 0x00, 0x00, 0x00, 0x7a };
    private readonly byte[] commandReadLastWeek = { 0xa6, 0x00, 0x00, 0x00, 0x00, 0x6a };
    private readonly byte[] commandReadLastYear = { 0xa8, 0x00, 0x00, 0x00, 0x00, 0x8a };
    private readonly byte[] commandShutdownDevice = { 0xa1, 0x00, 0x00, 0x00, 0x00, 0x1a };
    private readonly Guid feedCharacteristicGuid = new(feedCharacteristicUuid);
    private readonly Guid historyCharacteristicGuid = new(historyCharacteristicUuid);
    private readonly Guid sensorServiceGuid = new(sensorServiceUuid);

    public async Task<string> Connect(string deviceId)
    {
        var device = await this.FindDeviceAsync(deviceId, await Bluetooth.GetPairedDevicesAsync());

        if (device == null)
            device = await this.FindDeviceAsync(deviceId, await Bluetooth.ScanForDevicesAsync());

        if (device == null)
            throw new BluetoothDeviceNotFoundException(deviceId);

        await device.PairAsync();
        return device.Id;
    }

    private async Task<BluetoothDevice?> FindDeviceAsync(string deviceId,
        IReadOnlyCollection<BluetoothDevice> devices)
    {
        return devices.FirstOrDefault(d => d.Id == deviceId);
    }

    public async Task<string> AutoConnect()
    {
        var devices = await Bluetooth.GetPairedDevicesAsync();
        var device = devices.FirstOrDefault(item => item.Name.ToLower().Contains("tp358") && item.IsPaired);

        if (device != null)
            return device.Id;

        var discoveredDevices = await Bluetooth.ScanForDevicesAsync();
        foreach (var discoveredDevice in discoveredDevices)
            if (discoveredDevice.Name.ToLower().Contains("tp358"))
            {
                if (discoveredDevice.IsPaired) return discoveredDevice.Id;

                var options = new RequestDeviceOptions();
                options.AcceptAllDevices = true;


                var possibleDevice = await Bluetooth.RequestDeviceAsync(options);
                if (possibleDevice != null)
                    return possibleDevice.Id;
            }

        throw new BluetoothDeviceNotFoundException("Thermpro358");
    }

    private static async Task<GattCharacteristic> TryHardGettingService(List<GattService> serviceList, string charId)
    {
        foreach (var service in serviceList)
        {
            var characteristics = await service.GetCharacteristicsAsync();
            foreach (var characteristic in characteristics)
                if (characteristic.Uuid.ToString() == charId)
                    return characteristic;
        }

        await serviceList.First().Device.PairAsync();
        foreach (var service in serviceList)
        {
            var characteristics = await service.GetCharacteristicsAsync();
            foreach (var characteristic in characteristics)
                if (characteristic.Uuid.ToString() == charId)
                    return characteristic;
        }

        throw new ImpossibleBluetoothFuckeryException();
    }

    public async Task Fetch(BluetoothDeviceName deviceName, CancellationToken cancellationToken)
    {
        var gatt = await ConnectGattDevice(item => item.Name == deviceName);

        var sensorService = await gatt.GetPrimaryServiceAsync(this.sensorServiceGuid);
        var characteristic = await sensorService.GetCharacteristicAsync(this.feedCharacteristicGuid);

        AddNotification(characteristic, cancellationToken);
    }

    public async Task FetchLastDay(BluetoothDeviceId deviceId, CancellationToken cancellationToken)
    {
        var gatt = await ConnectGattDevice(item => item.Id == deviceId);
        var sensorService = await gatt.GetPrimaryServiceAsync(this.sensorServiceGuid);
        var characteristicList = await sensorService.GetCharacteristicsAsync();

        var characteristic = characteristicList.First(item => item.Uuid.ToString() == historyCharacteristicUuid);

        var characteristicFeed = await sensorService.GetCharacteristicAsync(this.feedCharacteristicGuid);
        await AddHistoryNotification(characteristicFeed, cancellationToken);

        await characteristic.WriteValueWithoutResponseAsync(this.commandReadLastDay);

        while (!cancellationToken.IsCancellationRequested && !allFetched) // todo add finished condition
            WaitForNotification();

        //if (toSave != null)
        //{
        //    SaveWhenLastPackageIsDone(toSave);
        //}

        // save as csv/json
    }

    private void SaveWhenLastPackageIsDone(string pathToSave)
    {
        throw new NotImplementedException();
    }

    public static async Task<RemoteGattServer> ConnectGattDevice(Func<BluetoothDevice, bool> deviceFilter)
    {
        var devices = await Bluetooth.GetPairedDevicesAsync();
        var device = devices.FirstOrDefault(deviceFilter);

        var gatt = device.Gatt;
        await gatt.ConnectAsync();
        return gatt;
    }

    public async Task Fetch(BluetoothDeviceId deviceId, CancellationToken cancellationToken)
    {
        var gatt = await ConnectGattDevice(item => item.Id == deviceId);

        var sensorService = await gatt.GetPrimaryServiceAsync(this.sensorServiceGuid);
        var characteristicList = await sensorService.GetCharacteristicsAsync();

        var characteristic = characteristicList.First(item => item.Uuid.ToString() == feedCharacteristicUuid);

        await AddNotification(characteristic, cancellationToken);
    }


    private static async Task AddHistoryNotification(GattCharacteristic characteristic,
        CancellationToken cancellationToken)
    {
        characteristic.CharacteristicValueChanged += FetchHIstoryNotificationIncoming;
        await characteristic.StartNotificationsAsync();
    }

    private static async Task AddNotification(GattCharacteristic characteristic, CancellationToken cancellationToken)
    {
        characteristic.CharacteristicValueChanged += FetchNotificationIncoming;
        await characteristic.StartNotificationsAsync();
        while (!cancellationToken.IsCancellationRequested) WaitForNotification();
    }

    private static void WaitForNotification()
    {
        Thread.Sleep(1000);
    }

    // todo add get-devices that outputs deviceid - devicename from current devices/scanned devices
    // json/csve export ermoeglichen 
    private static void FetchHIstoryNotificationIncoming(object? sender, GattCharacteristicValueChangedEventArgs e)
    {
        if (e.Value == null) return;
        if (e.Value is { Length: < 10 }) return;
        ParseSensorData(e.Value, DateTime.Now).ForEach(PrintHistoricalNotification);
    }

    public static void PrintHistoricalNotification(SensorReading sensorReading)
    {
        var defaultForeground = Console.ForegroundColor;
        var timestamp = sensorReading.Timestamp;

        var tempColor = sensorReading.TemperatureCelsius >= 30.0 ? ConsoleColor.Red : ConsoleColor.Green;
        var humidityColor = sensorReading.HumidityPercent >= 60 ? ConsoleColor.Red : ConsoleColor.Cyan;

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.ForegroundColor = defaultForeground;
        Console.Write(" - ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Notification: ");
        Console.ForegroundColor = defaultForeground;
        Console.Write("Temperature: ");
        Console.ForegroundColor = tempColor;
        Console.Write($"{sensorReading.TemperatureCelsius,3:0.0}");
        Console.ForegroundColor = defaultForeground;
        Console.Write("°C - Humidity:");
        Console.ForegroundColor = humidityColor;
        Console.Write($"{sensorReading.HumidityPercent,3:0}");
        Console.ForegroundColor = humidityColor;
        Console.Write("%\n");
        Console.ForegroundColor = defaultForeground;

        Console.ResetColor();
    }

    private static void FetchNotificationIncoming(object? sender, GattCharacteristicValueChangedEventArgs e)
    {
        if (e.Value == null) return;
        if (e.Value.Length != 7) return;
        double temp = e.Value[3] / 10;
        PrintDefaultNotification(temp, e.Value[5]);
    }

    public static void PrintDefaultNotification(double temperature, double humidity, DateTime? date = null)
    {
        var defaultForeground = Console.ForegroundColor;
        var timestamp = date ?? DateTime.Now;

        var tempColor = temperature >= 30.0 ? ConsoleColor.Red : ConsoleColor.Green;
        var humidityColor = humidity >= 60 ? ConsoleColor.Red : ConsoleColor.Cyan;

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.ForegroundColor = defaultForeground;
        Console.Write(" - ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("Notification: ");
        Console.ForegroundColor = defaultForeground;
        Console.Write("Temperature: ");
        Console.ForegroundColor = tempColor;
        Console.Write($"{temperature,3:0.0}");
        Console.ForegroundColor = defaultForeground;
        Console.Write("°C - Humidity:");
        Console.ForegroundColor = humidityColor;
        Console.Write($"{humidity,3:0}");
        Console.ForegroundColor = humidityColor;
        Console.Write("%\n");
        Console.ForegroundColor = defaultForeground;

        Console.ResetColor();
    }

    public static List<SensorReading> ParseSensorData(byte[] packet, DateTime referenceTime)
    {
        var valuesPerNotification = 5;
        var fieldsPerValue = 3;

        var result = new List<SensorReading>();

        var checkByte = packet[0];
        var endByte = packet[^1];

        if ((endByte != 122 && endByte != 138) || checkByte < 166 || checkByte > 168)
            return Array.Empty<SensorReading>().ToList();

        var packetIndex = packet[1] + packet[2] * 255 + packet[3] * 255;

        var intervalMinutes = checkByte switch
        {
            166 => 60, // 1 hour
            167 => 1, // 1 minute
            168 => 60, // 1 hour
            _ => throw new InvalidOperationException($"Unknown check byte {checkByte}")
        };
        var lastPackageIndex = checkByte switch
        {
            166 => 34,
            167 => 287,
            168 => 1752,
            _ => throw new InvalidOperationException($"Unknown check byte {checkByte}")
        };

        for (var i = 4; i < packet.Length - 1; i += 3)
        {
            var tempRaw = packet[i];
            var battery = packet[i + 1]; // unused
            var humidity = packet[i + 2];

            if (tempRaw == 255 && humidity == 255)
                continue;

            var lastPackage = lastPackageIndex == packetIndex;
            var lastElement = packet.Length + 3 <= i;

            var sensorReading = new SensorReading
            {
                Timestamp = DateTime.Now.AddMinutes((lastPackageIndex - packetIndex) * intervalMinutes * -1 -
                                                    (valuesPerNotification - (i - 4) / 3) * intervalMinutes),
                TemperatureCelsius = tempRaw / 10f,
                HumidityPercent = humidity,
                LastElement = lastElement && lastPackage
            };
            result.Add(sensorReading);
            if (lastElement) allFetched = true;
        }

        return result;
    }
}