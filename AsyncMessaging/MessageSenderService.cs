using AsyncMessaging;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using System.Text.Json;

public class MessageSenderService
{
    private readonly AzureServiceBusOptions _options;

    public MessageSenderService(IOptions<AzureServiceBusOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendMessageAsync(string user, string text)
    {
        var client = new ServiceBusClient(_options.ConnectionString);
        var sender = client.CreateSender(_options.QueueName);

        var payload = new Message { User = user, Text = text };
        var json = JsonSerializer.Serialize(payload);

        var message = new ServiceBusMessage(json)
        {
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message);
        Console.WriteLine("-----------------------------");
        Console.WriteLine($"Gesendet: {json}");
    }
}
