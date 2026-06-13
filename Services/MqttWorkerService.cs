using System.Text;
using System.Text.Json;
using API_WateringDashboard.Data;
using API_WateringDashboard.Models;
using MQTTnet;

namespace API_WateringDashboard.Services;

public class MqttWorkerService : BackgroundService
{
    private readonly ILogger<MqttWorkerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private IMqttClient? _mqttClient;

    public MqttWorkerService(
        ILogger<MqttWorkerService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _config = config;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttClientFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(
                _config["Mqtt:Broker"] ?? "localhost",
                int.Parse(_config["Mqtt:Port"] ?? "1883"))
            .WithCleanSession()
            .Build();

        //On message received
        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            _logger.LogInformation("Message received at [{Topic}]",topic);

            try
            {
                var data = JsonSerializer.Deserialize<JsonElement>(payload);

                 var actionType = data.TryGetProperty("type", out var t) ? t.GetString() : "";
                var origin = data.TryGetProperty("origin", out var o) ? o.GetString() ?? "UNKNOWN" : "UNKNOWN";

                if (actionType == "keep_alive")
                {
                    _logger.LogInformation("{Origin} is alive", origin);
                    return;
                }
                var reading = new Reading
                {
                    Timestamp = DateTime.UtcNow,
                    Origin = origin,
                    AirTemperature = GetDouble(data, "air_t"),
                    AirHumidity    = GetDouble(data, "air_h"),
                    SoilHumidity   = GetDouble(data, "soil_h"),
                    Status         = data.TryGetProperty("was_watered", out var s) ? s.GetString() : null
                };

                await SaveReadingAsync(reading);

                _logger.LogInformation(
                    "{Origin} | Temp: {T}°C | Hum_Ar: {H}% | Solo: {S}% | Status: {St}",
                    reading.Origin, reading.AirTemperature, reading.AirHumidity,
                    reading.SoilHumidity, reading.Status);
            }catch (JsonException)
            {
                _logger.LogInformation("Simple text: {Payload}", payload);
            }
        };

        // Connected to broker
        _mqttClient.ConnectedAsync += async e =>
        {
            _logger.LogInformation("Connected to MQTT Broker!");
            await _mqttClient.SubscribeAsync(
                new MqttTopicFilterBuilder()
                    .WithTopic(_config["Mqtt:Topic"] ?? "watering_data/#")
                    .Build());
            _logger.LogInformation("Listening: {Topic}", _config["Mqtt:Topic"]);
        };

        _mqttClient.DisconnectedAsync += async e =>
        {
            _logger.LogWarning("Desconnected to broker. Reconnecting in 5s...");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            try { await _mqttClient.ConnectAsync(options, stoppingToken); }
            catch { _logger.LogError("Failed to reconnect."); }
        };

        //Connect to broker
        await _mqttClient.ConnectAsync(options, stoppingToken);

        //Keeps it alive
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task SaveReadingAsync(Reading reading)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Readings.Add(reading);
        await db.SaveChangesAsync();
    }

    private static double? GetDouble(JsonElement el, string key)
    {
        if (el.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetDouble();
        return null;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient?.IsConnected == true)
            await _mqttClient.DisconnectAsync();
        await base.StopAsync(cancellationToken);
    }

}
