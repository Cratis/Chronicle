// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.given;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture;

public class when_running_a_scenario_with_scope_validation : Specification
{
    IServiceProvider _rootServices;
    IServiceProvider? _scenarioServices;
    IChronicleFixture _chronicleFixture;
    scenario_fixture _fixture;
    scope_lifetime? _scopeLifetime;
    Exception? _exception;

    void Establish()
    {
        var services = new ServiceCollection();
        var registry = new MutableServiceRegistry();
        services.AddSingleton(registry);
        services.AddScoped(_ => Substitute.For<IEventStore>());
        services.AddScoped<scope_lifetime>();
        var innerRootServices = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _rootServices = new FallbackServiceProvider(innerRootServices, registry);

        _chronicleFixture = Substitute.For<IChronicleFixture>();
        _chronicleFixture.RemoveAllDatabases(Arg.Any<IEnumerable<string>?>()).Returns(Task.CompletedTask);
        _fixture = new scenario_fixture(_chronicleFixture, new test_web_application_factory(_rootServices));
    }

    async Task Because()
    {
        try
        {
            await _fixture.InitializeAsync();
            _scenarioServices = _fixture.Services;
            _scopeLifetime = _scenarioServices.GetRequiredService<scope_lifetime>();
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
        finally
        {
            try
            {
                await _fixture.DisposeAsync();
            }
            catch (Exception exception)
            {
                _exception ??= exception;
            }
        }
    }

    void Destroy() => (_rootServices as IDisposable)?.Dispose();

    [Fact] void should_resolve_the_scoped_event_store() => _exception.ShouldBeNull();
    [Fact] void should_run_scenario_teardown() => _fixture.OnDisposeCalled.ShouldBeTrue();
    [Fact] void should_expose_services_through_the_fallback_provider_scope() => Assert.IsType<FallbackServiceProvider>(_scenarioServices);
    [Fact] void should_dispose_the_scenario_scope_asynchronously() => (_scopeLifetime?.IsDisposed ?? false).ShouldBeTrue();

    sealed class scenario_fixture(IChronicleFixture fixture, test_web_application_factory factory) : ChronicleClientFixture<IChronicleFixture>(fixture)
    {
        public bool OnDisposeCalled { get; private set; }

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

        protected override Task OnDisposeAsync()
        {
            OnDisposeCalled = true;
            return Task.CompletedTask;
        }
    }

    sealed class scope_lifetime : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
