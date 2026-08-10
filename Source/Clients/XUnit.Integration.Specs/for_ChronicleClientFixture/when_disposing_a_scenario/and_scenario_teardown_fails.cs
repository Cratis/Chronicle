// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.given;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.when_disposing_a_scenario;

public class and_scenario_teardown_fails : Specification
{
    IServiceProvider _rootServices;
    ITeardownChronicleFixture _chronicleFixture;
    scenario_fixture _fixture;
    Exception _teardownFailure;
    Exception _exception;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => Substitute.For<IEventStore>());
        _rootServices = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        _teardownFailure = new Exception("Scenario teardown failed");
        _chronicleFixture = Substitute.For<ITeardownChronicleFixture>();
        _chronicleFixture.RemoveAllDatabases(Arg.Any<IEnumerable<string>?>()).Returns(Task.CompletedTask);
        _fixture = new scenario_fixture(_chronicleFixture, new test_web_application_factory(_rootServices), _teardownFailure);
    }

    async Task Because()
    {
        await _fixture.InitializeAsync();
        _exception = await Catch.Exception(_fixture.DisposeAsync);
    }

    void Destroy() => (_rootServices as IDisposable)?.Dispose();

    [Fact] void should_rethrow_the_scenario_teardown_failure() => _exception.ShouldBeSame(_teardownFailure);
    [Fact] void should_remove_all_databases() => _chronicleFixture.Received(1).RemoveAllDatabases(Arg.Any<IEnumerable<string>?>());

    public interface ITeardownChronicleFixture : IChronicleFixture;

    sealed class scenario_fixture(ITeardownChronicleFixture fixture, test_web_application_factory factory, Exception teardownFailure) : ChronicleClientFixture<ITeardownChronicleFixture>(fixture)
    {
        public override async Task DisposeAsync()
        {
            try
            {
                await base.DisposeAsync();
            }
            finally
            {
                _webApplicationFactory = null;
            }
        }

        protected override IAsyncDisposable CreateWebApplicationFactory() => factory;

        protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
        {
        }

        protected override Task OnInitializeAsync() => EnsureBuilt();

        protected override Task OnDisposeAsync() => Task.FromException(teardownFailure);
    }
}
