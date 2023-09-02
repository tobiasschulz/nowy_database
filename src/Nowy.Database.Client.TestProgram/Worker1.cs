using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nowy.Database.Contract.Services;

namespace Nowy.Database.Client.TestProgram;

public class Worker1 : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _host_lifetime;
    private readonly INowyMessageHub _message_hub;
    private readonly INowyDatabase _database;
    private readonly Receiver1 _receiver1;

    public Worker1(
        ILogger<Worker1> logger,
        IHostApplicationLifetime host_lifetime,
        INowyMessageHub message_hub,
        INowyDatabase database,
        Receiver1 receiver1
    )
    {
        this._logger = logger;
        this._host_lifetime = host_lifetime;
        this._message_hub = message_hub;
        this._database = database;
        this._receiver1 = receiver1;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExecuteAsync start");

        await this.TestSend();

        this._host_lifetime.StopApplication();
    }

    public class Receiver1 : INowyMessageHubReceiver
    {
        private static readonly string[] EventNames = new[] { "test1", "test3", };

        internal readonly List<(string event_name, INowyMessageHubReceiverPayload payload)> ReceivedBuffer = new();

        IEnumerable<string> INowyMessageHubReceiver.GetEventNamePrefixes() => EventNames;

        Task INowyMessageHubReceiver.ReceiveMessageAsync(string event_name, INowyMessageHubReceiverPayload payload)
        {
            ReceivedBuffer.Add(( event_name, payload ));
            return Task.CompletedTask;
        }
    }

    public async Task TestSend()
    {
        await Task.Delay(TimeSpan.FromSeconds(2));

        await _message_hub.BroadcastMessageAsync("abc", new object[] { new List<int> { 1, 2, 3, } }, new NowyMessageOptions());


        await _message_hub.BroadcastMessageAsync("test1", new object[] { new List<int> { 1, 2, 3, } }, new NowyMessageOptions());

        List<Worker1_TestModel> received_5 = new();
        List<Worker1_TestModel> received_3 = new();
        List<Worker1_TestModel> received_all = new();

        _message_hub.SubscribeEvent<Worker1_TestModel>(config =>
        {
            config.Where(o => o.Fuck_1 == 5);
            config.AddHandler(o =>
            {
                received_5.Add(o);
                this._logger.LogInformation("Received event with '5' receiver: {model}", o);
            });
        });

        _message_hub.SubscribeEvent<Worker1_TestModel>(config =>
        {
            config.Where(o => o.Fuck_1 == 3);
            config.AddHandler(o =>
            {
                received_3.Add(o);
                this._logger.LogInformation("Received event with '3' receiver: {model}", o);
            });
        });

        _message_hub.SubscribeEvent<Worker1_TestModel>(config =>
        {
            config.AddHandler(o =>
            {
                received_all.Add(o);
                this._logger.LogInformation("Received event with 'all' receiver: {model}", o);
            });
        });

        _message_hub.QueueEvent(config =>
        {
            config.AddValue(new Worker1_TestModel()
            {
                Fuck_1 = 3, Fuck_2 = "three",
            });
            config.AddValue(new Worker1_TestModel()
            {
                Fuck_1 = 8, Fuck_2 = "eight",
            });
        });
        _message_hub.QueueEvent(config =>
        {
            config.AddValue(new Worker1_TestModel()
            {
                Fuck_1 = 5, Fuck_2 = "five",
            });
        });
        _message_hub.QueueEvent(config =>
        {
            config.AddValue(new Worker1_TestModel()
            {
                Fuck_1 = 9, Fuck_2 = "nine",
            });
        });

        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}

public class Worker1_TestModel
{
    public long Fuck_1 { get; set; }
    public string Fuck_2 { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}
