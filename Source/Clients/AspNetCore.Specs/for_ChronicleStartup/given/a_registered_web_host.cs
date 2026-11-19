// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Registrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.for_ChronicleStartup.given;

public class a_registered_web_host : Specification
{
    protected startup_logs _logs;
    protected IHostApplicationLifetime _lifetime;
    protected IChronicleClient _client;
    protected IEventStore _eventStore;
    protected IChronicleConnection _connection;
    protected ServiceProvider _services;
    protected IApplicationBuilder _app;
    protected IHostedService _startup;
    protected CancellationTokenSource _started;
    protected CancellationTokenSource _stopping;
    protected TaskCompletionSource<IEventStore> _storeCompletion;
    protected TaskCompletionSource _connectCalled;
    protected TaskCompletionSource _connectCompletion;
    protected Task _startedSignal = Task.CompletedTask;

    void Establish()
    {
        _client = Substitute.For<IChronicleClient>();
        _eventStore = Substitute.For<IEventStore>();
        _connection = Substitute.For<IChronicleConnection>();
        _started = new();
        _stopping = new();
        _storeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _connectCalled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _connectCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _lifetime = Substitute.For<IHostApplicationLifetime>();
        _lifetime.ApplicationStarted.Returns(_started.Token);
        _lifetime.ApplicationStopping.Returns(_stopping.Token);
        _client.Options.Returns(new ChronicleOptions());
        _connection.Lifecycle.Returns(Substitute.For<IConnectionLifecycle>());
        _eventStore.Connection.Returns(_connection);
        _eventStore.Registration.Returns(RegistrationOutcome.NotRun);
        _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(Task.FromResult(_eventStore));
        _connection.Connect().Returns(_ =>
        {
            _connectCalled.TrySetResult();
            return _connectCompletion.Task;
        });

        _logs = new();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(_logs);
        builder.Configuration.Sources.Clear();
        builder.AddCratisChronicle(options => options.EventStore = "the-store");
        builder.Services.AddSingleton(_client);
        builder.Services.AddSingleton(_lifetime);
        _services = builder.Services.BuildServiceProvider();
        _app = new ApplicationBuilder(_services);
        _startup = _services.GetServices<IHostedService>().Single(service => service.GetType().Name == "ChronicleClientStartup");
    }

    protected async Task SignalStarted()
    {
        await _startup.StartAsync(CancellationToken.None);
        _startedSignal = Task.Run(() => _started.CancelAsync());
        await _connectCalled.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    async Task Destroy()
    {
        _storeCompletion.TrySetResult(_eventStore);
        _connectCompletion.TrySetResult();
        try
        {
            await _startedSignal.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await StopAndDisposeHost();
        }
    }

    async Task StopAndDisposeHost()
    {
        try
        {
            await _stopping.CancelAsync();
            await _startup.StopAsync(CancellationToken.None);
        }
        finally
        {
            try
            {
                await _services.DisposeAsync();
            }
            finally
            {
                _started.Dispose();
                _stopping.Dispose();
            }
        }
    }
}
