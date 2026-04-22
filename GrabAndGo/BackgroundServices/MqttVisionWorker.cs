using GrabAndGo.Application.DTOs.Vision;
using GrabAndGo.Services.Interfaces;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Text;
using System.Text.Json;

namespace GrabAndGo.Api.BackgroundServices
{
    public class MqttVisionWorker : BackgroundService
    {
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _mqttOptions;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MqttVisionWorker> _logger;
        public MqttVisionWorker(IServiceScopeFactory scopeFactory, ILogger<MqttVisionWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttOptions = new MqttClientOptionsBuilder()
                .WithClientId($"GrabAndGo_Backend_{Guid.NewGuid()}") // Unique ID per boot
                .WithTcpServer("broker.hivemq.com", 1883)            // Public sandbox
                .WithCleanSession(true)                              // Start fresh every time
                .Build();
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        }
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("MQTT Vision Worker is starting.");

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_mqttClient.IsConnected)
                {
                    try
                    {
                        await _mqttClient.ConnectAsync(_mqttOptions, cancellationToken);
                        _logger.LogInformation("Successfully connected to HiveMQ MQTT Broker.");

                        var mqttSubscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
                            .WithTopicFilter(f => f.WithTopic("grabandgo/+/store/+/vision/events/#").WithAtLeastOnceQoS())
                            .Build();

                        await _mqttClient.SubscribeAsync(mqttSubscribeOptions, cancellationToken);
                        _logger.LogInformation("Subscribed to GrabAndGo vision topics.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to connect to MQTT broker. Retrying in 5 seconds...");
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
        public async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                string jsonString = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
                _logger.LogInformation("Received MQTT Event on topic {Topic}", e.ApplicationMessage.Topic);

                VisionEventRequestDto visionEvent = JsonSerializer.Deserialize<VisionEventRequestDto>(jsonString);

                if (visionEvent != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();

                    await cartService.ProcessVisionEventAsync(visionEvent);

                    _logger.LogInformation("Successfully processed Vision Event for TrackId: {TrackId} Time: {Now}", visionEvent.TrackId,DateTime.Now);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message on topic: {Topic}", e.ApplicationMessage.Topic);
            }
        }
        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MQTT Vision Worker is stopping.");
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptionsBuilder().Build(), stoppingToken);
            }
            await base.StopAsync(stoppingToken);
        }
    }
}