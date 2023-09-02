using Microsoft.Extensions.DependencyInjection;
using Nowy.Database.Common.Models;
using Nowy.Database.Contract.Models;
using Nowy.Database.Contract.Services;
using Nowy.MessageHub.Client;
using Serilog;
using Xunit.Abstractions;

namespace Nowy.Database.Client.Tests.Tests;

public class UnitTest3
{
    private readonly ServiceProvider _sp;

    public UnitTest3(ITestOutputHelper output)
    {
        Log.Logger = new LoggerConfiguration()
            // add the xunit test output sink to the serilog logger
            // https://github.com/trbenning/serilog-sinks-xunit#serilog-sinks-xunit
            .WriteTo.TestOutput(output)
            .CreateLogger();

        ServiceCollection services = new();

        services.AddHttpClient();

        services.AddNowyDatabaseClient(config => { config.EndpointUrl = "https://main.database.schulz.dev"; });
        services.AddNowyMessageHubClient(config => { config.AddEndpoint("https://main.messagehub.schulz.dev"); });

        this._sp = services.BuildServiceProvider();
    }

    [Fact]
    public async Task TestFilter1()
    {
        INowyDatabase database = this._sp.GetRequiredService<INowyDatabase>();

        INowyCollection<TestModel> collection = database.GetCollection<TestModel>("unit_tests");

        TestModel a;
        IReadOnlyList<TestModel> filtered_result;

        a = await collection.UpsertAsync("456", new TestModel() { Fuck = "A" });
        Assert.Equal("A", a.Fuck);

        filtered_result = await collection.GetByFilterAsync(
            ModelFilterBuilder.And(
                ModelFilterBuilder.Equals(nameof(TestModel.id), "456")
            ).Build()
        );
        Assert.NotEmpty(filtered_result);

        filtered_result = await collection.GetByFilterAsync(
            ModelFilterBuilder.And(
                ModelFilterBuilder.Equals(nameof(TestModel.id), "456"),
                ModelFilterBuilder.Equals(nameof(TestModel.Fuck), "A")
            ).Build()
        );
        Assert.NotEmpty(filtered_result);

        filtered_result = await collection.GetByFilterAsync(
            ModelFilterBuilder.And(
                ModelFilterBuilder.Equals(nameof(TestModel.id), "456"),
                ModelFilterBuilder.Equals(nameof(TestModel.Fuck), "XYZ")
            ).Build()
        );
        Assert.Empty(filtered_result);

        a = await collection.UpsertAsync("456", new TestModel() { Fuck = "B" });
        Assert.Equal("B", a.Fuck);

        filtered_result = await collection.GetByFilterAsync(
            ModelFilterBuilder.And(
                ModelFilterBuilder.Equals(nameof(TestModel.id), "456"),
                ModelFilterBuilder.Equals(nameof(TestModel.Fuck), "B")
            ).Build()
        );
        Assert.NotEmpty(filtered_result);

        filtered_result = await collection.GetByFilterAsync(
            ModelFilterBuilder.And(
                ModelFilterBuilder.Equals(nameof(TestModel.id), "456"),
                ModelFilterBuilder.Equals(nameof(TestModel.Fuck), "XYZ")
            ).Build()
        );
        Assert.Empty(filtered_result);

        filtered_result = await collection.GetByFilterAsync(
            ModelFilterBuilder.And(
                ModelFilterBuilder.In(nameof(TestModel.id), new[] { "456", }),
                ModelFilterBuilder.In(nameof(TestModel.Fuck), new[] { "B", })
            ).Build()
        );
        Assert.NotEmpty(filtered_result);

        filtered_result = await collection.GetByFilterAsync(
            ModelFilterBuilder.And(
                ModelFilterBuilder.In(nameof(TestModel.id), new[] { "456", }),
                ModelFilterBuilder.In(nameof(TestModel.Fuck), new[] { "XYZ", })
            ).Build()
        );
        Assert.Empty(filtered_result);
    }

    public sealed class TestModel : BaseModel
    {
        public string? FuckTest { get; set; }
        public string? Fuck { get; set; }
        public string? Test1 { get; set; }
        public string? Test2 { get; set; }
        public string? Test3 { get; set; }
    }
}
