// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Captures.Engine.DeclarationLanguage;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;

namespace Cratis.Chronicle.Captures.for_CapturesManager.given;

public class a_manager_with_blocked_source_resolution : for_CaptureEventsSubscriptions.given.a_capture_events_subscriptions
{
    protected readonly TaskCompletionSource _resolving = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly List<string> _operations = [];
    protected CapturesManager _manager;
    protected TestKitSilo _silo;
    protected bool _subscribed;
    protected bool _teardownWaited;
    protected bool _exists = true;
    protected Capture _capture;

    async Task Establish()
    {
        _capture = new(_definition.Id, _definition.Name, new CaptureDeclaration("events-declaration"), CaptureStatus.Started);
        _captures.GetAll().Returns(_ => _exists ? [_capture] : []);
        _captures.Has(_definition.Id).Returns(_ => _exists);
        _captures.Get(_definition.Id).Returns(_ => _capture);
        _captures.Save(Arg.Any<Capture>()).Returns(call =>
        {
            _capture = call.Arg<Capture>();
            return Task.CompletedTask;
        });
        _captures.Delete(_definition.Id).Returns(_ =>
        {
            _exists = false;
            _operations.Add("delete");
            return Task.CompletedTask;
        });

        var observer = Substitute.For<IObserver>();
        observer.GetState().Returns(new ObserverState());
        observer.IsSubscriptionEstablished().Returns(_ => _subscribed);
        AnyNeedsRecovery(observer).Returns(true);
        AnyRecover(observer).Returns(_ =>
        {
            _operations.Add("subscribe");
            _subscribed = true;
            return Task.FromResult<IEnumerable<Concepts.Events.EventType>>([]);
        });
        observer.Unsubscribe().Returns(_ =>
        {
            _operations.Add("unsubscribe");
            _subscribed = false;
            return Task.CompletedTask;
        });
        observer.Remove().Returns(_ =>
        {
            _operations.Add("remove");
            return Task.CompletedTask;
        });
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(observer);

        var language = Substitute.For<ILanguageService>();
        language.Compile("events-declaration").Returns(_definition);
        _silo = new();
        _silo.AddService(_storage);
        _silo.AddService(language);
        _silo.AddService(Substitute.For<Engine.ICaptureValidator>());
        _silo.AddService<ICaptureEventsSubscriptions>(_subscriptions);
        _silo.AddService<Microsoft.Extensions.Logging.ILogger<CapturesManager>>(NullLogger<CapturesManager>.Instance);
        _silo.AddProbe(_ => observer);
        _manager = await _silo.CreateGrainAsync<CapturesManager>(_eventStore.Value);

        // This is the actual ResolveFor call made by CaptureEventsSubscriptions AFTER Started eligibility
        // and the manager-side observer hint. Stop/Delete must drain it, not race a second status snapshot.
        var schema = await _eventTypes.GetFor(new Concepts.Events.EventTypeId("ShipmentDispatched"));
        _eventTypes.GetFor(new Concepts.Events.EventTypeId("ShipmentDispatched"), Arg.Any<Concepts.Events.EventTypeGeneration?>()).Returns(async _ =>
        {
            _resolving.TrySetResult();
            await _release.Task;
            return schema;
        });
    }

    protected async Task StopDuring(Task running)
    {
        await _resolving.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var stop = _manager.Stop(_definition.Id);
        _teardownWaited = !stop.IsCompleted;
        var blockedPass = _manager.EnsureSubscribedIn("blocked-tenant");
        _release.TrySetResult();
        await Task.WhenAll(running, stop, blockedPass).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _manager.Ensure();
        await _silo.TimerRegistry.FireAllAsync();
    }

    protected async Task DeleteDuring(Task running)
    {
        await _resolving.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var delete = _manager.Delete(_definition.Id);
        _teardownWaited = !delete.IsCompleted;
        var blockedPass = _manager.EnsureSubscribedIn("blocked-tenant");
        _release.TrySetResult();
        await Task.WhenAll(running, delete, blockedPass).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _manager.Ensure();
        await _silo.TimerRegistry.FireAllAsync();
    }
}
