using AsyncMessaging;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using System.Text.Json;

public class MessageReceiverService : BackgroundService
{
    private readonly AzureServiceBusOptions _options;

    public MessageReceiverService(IOptions<AzureServiceBusOptions> options)
    {
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var client = new ServiceBusClient(_options.ConnectionString);
        var processor = client.CreateProcessor(_options.QueueName, new ServiceBusProcessorOptions());

        processor.ProcessMessageAsync += MessageHandler;
        processor.ProcessErrorAsync += ErrorHandler;

        await processor.StartProcessingAsync(stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
        await processor.StopProcessingAsync();
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();

        try
        {
            var message = JsonSerializer.Deserialize<Message>(body);
            Console.WriteLine($"Empfangen: User={message?.User}, Text={message?.Text}");
            Console.WriteLine("-----------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler beim Verarbeiten: {ex.Message}");
        }

        await args.CompleteMessageAsync(args.Message);
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"Empfangsfehler: {args.Exception.Message}");
        return Task.CompletedTask;
    }
}
