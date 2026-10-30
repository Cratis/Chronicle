// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.given;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.when_disposing_through_the_v3_contract;

/// <summary>
/// Verifies that the ValueTask adapter preserves cleanup aggregation and the virtual Task hook.
/// </summary>
public class and_cleanup_steps_fail : Specification
{
    ServiceProvider _rootServices;
    failing_fixture _fixture;
    Exception _teardownFailure;
    Exception _scopeFailure;
    Exception _exception;

    void Establish()
    {
        _teardownFailure = new Exception("Scenario teardown failed");
        _scopeFailure = new Exception("Scope disposal failed");
        var services = new ServiceCollection();
        services.AddScoped(_ => Substitute.For<IEventStore>());
        services.AddScoped(_ => new failing_scope(_scopeFailure));
        _rootServices = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var chronicle = Substitute.For<IAggregationChronicleFixture>();
        chronicle.RemoveAllDatabases(Arg.Any<IEnumerable<string>?>()).Returns(Task.CompletedTask);
        _fixture = new failing_fixture(chronicle, new test_web_application_factory(_rootServices), _teardownFailure);
    }

    async Task Because()
    {
        await ((IAsyncLifetime)_fixture).InitializeAsync();
        _ = _fixture.Services.GetRequiredService<failing_scope>();
        _exception = await Catch.Exception(async () => await ((IAsyncDisposable)_fixture).DisposeAsync());
    }

    async Task Destroy() => await _rootServices.DisposeAsync();

    [Fact] void should_reach_the_virtual_task_dispose_hook_once() => _fixture.DisposeCount.ShouldEqual(1);
    [Fact] void should_aggregate_both_cleanup_failures() => Assert.IsType<AggregateException>(_exception).InnerExceptions.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_teardown_failure() => Assert.IsType<AggregateException>(_exception).InnerExceptions[0].ShouldBeSame(_teardownFailure);
    [Fact] void should_preserve_the_scope_failure() => Assert.IsType<AggregateException>(_exception).InnerExceptions[1].ShouldBeSame(_scopeFailure);

    /// <summary>
    /// Isolates this fixture's shared factory.
    /// </summary>
    public interface IAggregationChronicleFixture : IChronicleFixture;

    sealed class failing_fixture(IAggregationChronicleFixture chronicle, test_web_application_factory factory, Exception teardownFailure) : ChronicleClientFixture<IAggregationChronicleFixture>(chronicle)
    {
        public int DisposeCount { get; private set; }

        public override async Task DisposeAsync()
        {
            DisposeCount++;
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

    sealed class failing_scope(Exception failure) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.FromException(failure);
    }
}
