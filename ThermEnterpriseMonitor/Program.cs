using Cocona;
using ThermEnterpriseMonitor;

//todo
// add loggin with verbose/debug 
// use tryhardservice cause this service throws exceptions like its coins 
// add greened commands, maybe some of them 
// better output with some (console?)lib
// adding all/more commands from thermpro358
// adding unit test for parsing 
// using something more for console commands, with nullable param and enums maybe, async   
// connect and autoconnect fixxen x)

var builder = CoconaApp.CreateBuilder();
var app = builder.Build();

app.AddCommand("connect", async (string deviceId) => await new BluetoothConnection().Connect(deviceId))
    .WithDescription("connects to device using provided id");

app.AddCommand("autoConnect", async () => await new BluetoothConnection().AutoConnect())
    .WithDescription("auto connects to first valid device");

app.AddCommand("fetchByDeviceId",
        async (string deviceId, CoconaAppContext ctx) =>
            await new BluetoothConnection().Fetch(new BluetoothDeviceId(deviceId), ctx.CancellationToken))
    .WithDescription(
        "fetches current stream from provided device; fetches {temperature and humidity};sample: fetch-by-device-id --device-id \"E6C08209BBF9\"");

app.AddCommand("fetchByDeviceName",
        async (string deviceName, CoconaAppContext ctx) =>
            await new BluetoothConnection().Fetch(new BluetoothDeviceName(deviceName), ctx.CancellationToken))
    .WithDescription("fetches current stream from provided device; fetches {temperature and humidity}");

app.AddCommand("fetchHistoryLastYear",
        async (string deviceId, CoconaAppContext ctx) =>
            await new BluetoothConnection().FetchLastDay(new BluetoothDeviceId(deviceId), ctx.CancellationToken))
    .WithDescription(
        "fetches hourly data from last year or til last save; sample: fetch-History-Last-Year --device-id \"E6C08209BBF9\"");

//app.AddCommand("sendCommand", (string deviceId, string characteristicsId, string notifitcationCharacteristicsId,string command, CoconaAppContext ctx) => new BluetoothConnection().FetchLastDay(new BluetoothDeviceId(deviceId), ctx.CancellationToken, toSave))
//    .WithDescription("send direct command to charastericsId from deviceId and fetches data from notification");
//app.AddCommand("saveHistoryLastDay", (string deviceId, CoconaAppContext ctx) => new BluetoothConnection().FetchLastDay(new BluetoothDeviceId(deviceId), ctx.CancellationToken, toSave))
//    .WithDescription("fetches hourly data from last year or til last save; sample: fetch-History-Last-Day --device-id \"E6C08209BBF9\"");
//app.AddCommand("fetchHistoryLastWeek", (string deviceId, CoconaAppContext ctx) => new BluetoothConnection().FetchLastYear(new BluetoothDeviceId(deviceId), ctx.CancellationToken))
//    .WithDescription("fetches hourly data from last year or til last save; sample: fetch-History --device-id \"E6C08209BBF9\"");
//app.AddCommand("fetchHistoryLastYear", (string deviceId, CoconaAppContext ctx) => new BluetoothConnection().FetchLastYear(new BluetoothDeviceId(deviceId), ctx.CancellationToken))
//    .WithDescription("fetches hourly data from last year or til last save; sample: fetch-History --device-id \"E6C08209BBF9\"");
//app.AddCommand("disableDevice", (string deviceId, CoconaAppContext ctx) => new BluetoothConnection().FetchLastYear(new BluetoothDeviceId(deviceId), ctx.CancellationToken))
//    .WithDescription("fetches hourly data from last year or til last save; sample: fetch-History --device-id \"E6C08209BBF9\"");
//app.AddCommand("TrollBattery", (string deviceId, CoconaAppContext ctx) => new BluetoothConnection().FetchLastYear(new BluetoothDeviceId(deviceId), ctx.CancellationToken))
//    .WithDescription("trolls the device");
//app.AddCommand("GetDevices", (string deviceId, CoconaAppContext ctx) => new BluetoothConnection().FetchLastYear(new BluetoothDeviceId(deviceId), ctx.CancellationToken))
//    .WithDescription("returns device names and ids ");

app.Run();