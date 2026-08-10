// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.given;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.when_disposing_a_scenario;

public class and_async_scoped_service_disposal_fails : Specification
{
    IServiceProvider _rootServices;
    IScopeDisposalChronicleFixture _chronicleFixture;
    scenario_fixture _fixture;
    throwing_scope_lifetime _scopeLifetime;
    Exception _scopeDisposalFailure;
    Exception _exception;

    void Establish()
    {
        _scopeDisposalFailure = new Exception("Scope disposal failed");

        var services = new ServiceCollection();
        services.AddScoped(_ => Substitute.For<IEventStore>());
        services.AddScoped(_ => new throwing_scope_lifetime(_scopeDisposalFailure));
        _rootServices = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        _chronicleFixture = Substitute.For<IScopeDisposalChronicleFixture>();
        _chronicleFixture.RemoveAllDatabases(Arg.Any<IEnumerable<string>?>()).Returns(Task.CompletedTask);
        _fixture = new scenario_fixture(_chronicleFixture, new test_web_application_factory(_rootServices));
    }

    async Task Because()
    {
        await _fixture.InitializeAsync();
        _scopeLifetime = _fixture.Services.GetRequiredService<throwing_scope_lifetime>();
        _exception = await Catch.Exception(_fixture.DisposeAsync);
    }

    void Destroy() => (_rootServices as IDisposable)?.Dispose();

    [Fact] void should_attempt_async_scope_disposal() => _scopeLifetime.DisposeCalled.ShouldBeTrue();
    [Fact] void should_rethrow_the_scope_disposal_failure() => _exception.ShouldBeSame(_scopeDisposalFailure);
    [Fact] void should_remove_all_databases() => _chronicleFixture.Received(1).RemoveAllDatabases(Arg.Any<IEnumerable<string>?>());

    public interface IScopeDisposalChronicleFixture : IChronicleFixture;

    sealed class scenario_fixture(IScopeDisposalChronicleFixture fixture, test_web_application_factory factory) : ChronicleClientFixture<IScopeDisposalChronicleFixture>(fixture)
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
    }

    sealed class throwing_scope_lifetime(Exception disposalFailure) : IAsyncDisposable
    {
        public bool DisposeCalled { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCalled = true;
            return ValueTask.FromException(disposalFailure);
        }
    }
}
