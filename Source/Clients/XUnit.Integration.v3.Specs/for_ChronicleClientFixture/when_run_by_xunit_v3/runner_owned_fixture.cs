// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.given;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration.for_ChronicleClientFixture.when_run_by_xunit_v3;

/// <summary>
/// A container-free fixture whose teardown assertions are executed by the real v3 runner.
/// </summary>
public sealed class runner_owned_fixture : ChronicleClientFixture<runner_owned_fixture.IRunnerChronicleFixture>
{
    readonly ServiceProvider _rootServices;
#pragma warning disable CA2213 // The base fixture owns and disposes the factory returned from CreateWebApplicationFactory
    readonly test_web_application_factory _factory;
#pragma warning restore CA2213
    int _disposeCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="runner_owned_fixture"/> class.
    /// </summary>
    public runner_owned_fixture()
        : base(Substitute.For<IRunnerChronicleFixture>())
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => Substitute.For<IEventStore>());
        services.AddScoped(_ => new scope_lifetime());
        _rootServices = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _factory = new test_web_application_factory(_rootServices);
        ChronicleFixture.RemoveAllDatabases(Arg.Any<IEnumerable<string>?>()).Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Gets the number of Task-based initialization hook calls.
    /// </summary>
    public int InitializeCount { get; private set; }

    /// <summary>
    /// Gets whether the scenario scope is alive.
    /// </summary>
    public bool ScopeIsAlive => !Services.GetRequiredService<scope_lifetime>().IsDisposed;

    /// <inheritdoc/>
#pragma warning disable CA2215 // Throwing proves the v3 runner never takes the synchronous disposal path
    public override void Dispose() => throw new InvalidOperationException("xUnit v3 must use only async fixture disposal");
#pragma warning restore CA2215

    /// <inheritdoc/>
    public override async Task DisposeAsync()
    {
        var lifetime = Services.GetRequiredService<scope_lifetime>();
        _disposeCount++;
        try
        {
            await base.DisposeAsync();
            _disposeCount.ShouldEqual(1);
            InitializeCount.ShouldEqual(1);
            lifetime.IsDisposed.ShouldBeTrue();
            await ChronicleFixture.Received(1).RemoveAllDatabases(Arg.Any<IEnumerable<string>?>());
        }
        finally
        {
            _webApplicationFactory = null;
            await _rootServices.DisposeAsync();
        }
    }

    /// <inheritdoc/>
    protected override IAsyncDisposable CreateWebApplicationFactory() => _factory;

    /// <inheritdoc/>
    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
    }

    /// <inheritdoc/>
    protected override async Task OnInitializeAsync()
    {
        InitializeCount++;
        await EnsureBuilt();
        _ = Services.GetRequiredService<scope_lifetime>();
    }

    /// <summary>
    /// Isolates the shared fixture factory from other specs.
    /// </summary>
    public interface IRunnerChronicleFixture : IChronicleFixture;

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
