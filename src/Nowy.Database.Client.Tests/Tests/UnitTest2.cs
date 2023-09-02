using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nowy.Database.Common;
using Nowy.Database.Common.Models;
using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;
using Nowy.MessageHub.Client;
using Serilog;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Nowy.Database.Client.Tests.Tests;

public class UnitTest2
{
    private readonly ServiceProvider _sp;
    private readonly ILogger _logger;

    public UnitTest2(ITestOutputHelper output)
    {
        Log.Logger = new LoggerConfiguration()
            // add the xunit test output sink to the serilog logger
            // https://github.com/trbenning/serilog-sinks-xunit#serilog-sinks-xunit
            .WriteTo.TestOutput(output)
            .CreateLogger();

        ServiceCollection services = new();

        services.AddHttpClient();

        services.AddNowyDatabaseClient(config => { config.EndpointUrl = "https://main.database.schulz.dev"; });
        services.AddNowyMessageHubClient(config =>
        {
            config.AddEndpoint("https://main.messagehub.schulz.dev");
            config.AddReceiver<Receiver1>(sp => new Receiver1());
        });

        this._sp = services.BuildServiceProvider();
        this._logger = this._sp.GetRequiredService<ILogger<UnitTest2>>();
    }

    private Receiver1 _receiver1 => _sp.GetRequiredService<Receiver1>();

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

    [Fact]
    public async Task TestSend1()
    {
        IHostedService[] hosted_services = _sp.GetRequiredService<IEnumerable<IHostedService>>().ToArray();
        Assert.NotEmpty(hosted_services);
        foreach (IHostedService hosted_service in hosted_services)
        {
            using CancellationTokenSource cts = new();
            await hosted_service.StartAsync(cts.Token);
        }

        try
        {
            INowyMessageHub hub = this._sp.GetRequiredService<INowyMessageHub>();

            await hub.WaitUntilConnectedAsync("test1", TimeSpan.FromMilliseconds(5000));

            await hub.BroadcastMessageAsync("abc", new object[] { new List<int> { 1, 2, 3, }, }, new NowyMessageOptions());

            Assert.Empty(_receiver1.ReceivedBuffer);

            await hub.BroadcastMessageAsync("test1", new object[] { new List<int> { 1, 2, 3, } }, new NowyMessageOptions());

            for (int i = 0; i < 50; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                if (_receiver1.ReceivedBuffer.Count != 0)
                    break;
            }

            Assert.NotEmpty(_receiver1.ReceivedBuffer);
        }
        finally
        {
            foreach (IHostedService hosted_service in hosted_services)
            {
                using CancellationTokenSource cts = new();
                await hosted_service.StopAsync(cts.Token);
            }
        }
    }

    public sealed class TestModel : BaseModel
    {
        public int Fuck_1 { get; set; }
        public string? Fuck_2 { get; set; }
        public string? Test1 { get; set; }
        public string? Test2 { get; set; }
        public string? Test3 { get; set; }
    }

    [Fact]
    public async Task TestEvents1()
    {
        IHostedService[] hosted_services = _sp.GetRequiredService<IEnumerable<IHostedService>>().ToArray();
        Assert.NotEmpty(hosted_services);
        foreach (IHostedService hosted_service in hosted_services)
        {
            using CancellationTokenSource cts = new();
            await hosted_service.StartAsync(cts.Token);
        }

        try
        {
            INowyMessageHub hub = this._sp.GetRequiredService<INowyMessageHub>();

            await hub.WaitUntilConnectedAsync("test1", TimeSpan.FromMilliseconds(5000));

            List<TestModel> received_5 = new();
            List<TestModel> received_3 = new();
            List<TestModel> received_all = new();

            hub.SubscribeEvent<TestModel>(config =>
            {
                config.Where(o => o.Fuck_1 == 5);
                config.AddHandler(o =>
                {
                    received_5.Add(o);
                    this._logger.LogInformation("Received event with '5' receiver: {model}", o);
                });
            });

            hub.SubscribeEvent<TestModel>(config =>
            {
                config.Where(o => o.Fuck_1 == 3);
                config.AddHandler(o =>
                {
                    received_3.Add(o);
                    this._logger.LogInformation("Received event with '3' receiver: {model}", o);
                });
            });

            hub.SubscribeEvent<TestModel>(config =>
            {
                config.AddHandler(o =>
                {
                    received_all.Add(o);
                    this._logger.LogInformation("Received event with 'all' receiver: {model}", o);
                });
            });

            hub.QueueEvent(config =>
            {
                config.AddValue(new TestModel()
                {
                    Fuck_1 = 3, Fuck_2 = "three",
                });
                config.AddValue(new TestModel()
                {
                    Fuck_1 = 8, Fuck_2 = "eight",
                });
            });
            hub.QueueEvent(config =>
            {
                config.AddValue(new TestModel()
                {
                    Fuck_1 = 5, Fuck_2 = "five",
                });
            });
            hub.QueueEvent(config =>
            {
                config.AddValue(new TestModel()
                {
                    Fuck_1 = 9, Fuck_2 = "nine",
                });
            });

            for (int i = 0; i < 50; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                if (received_all.Count == 4)
                    break;
            }

            Assert.NotEmpty(received_all);
            Assert.Equal(new[] { "three", "eight", "five", "nine", }.Order(), received_all.Select(o => o.Fuck_2).Order());
            Assert.Equal(new[] { "three", }.Order(), received_3.Select(o => o.Fuck_2).Order());
            Assert.Equal(new[] { "five", }.Order(), received_5.Select(o => o.Fuck_2).Order());
        }
        finally
        {
            foreach (IHostedService hosted_service in hosted_services)
            {
                using CancellationTokenSource cts = new();
                await hosted_service.StopAsync(cts.Token);
            }
        }
    }
}
