// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Observation.Reducers;
using Cratis.Chronicle.Reducers;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_EventStore.when_reregistering_after_disconnect.given;

public class a_store_with_an_explicit_reducer : when_preparing_for_registration.given.an_explicit_store
{
    protected IReducerHandler _original;
    protected virtual bool AutomaticRegistration => false;

    async Task Establish()
    {
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        var activated = new ActivatedArtifact(new Classic(), typeof(Classic), NullLogger<ActivatedArtifact>.Instance);
        _activator.Activate(Arg.Any<IServiceProvider>(), typeof(Classic)).Returns(activated);
        _services.Reducers.Observe(Arg.Any<IObservable<ReducerMessage>>(), Arg.Any<CallContext>()).Returns(Observable.Never<ReduceOperationMessage>());
        _auto = AutomaticRegistration;
        _store = CreateStore();
        await _store.DiscoverAll();
        await _connection.Lifecycle.Connected();
        _original = await _store.Reducers.Register(typeof(Classic));
    }

    void Destroy()
    {
        foreach (var handler in _store.Reducers.GetAllHandlers())
        {
            handler.Disconnect();
            (handler as IDisposable)?.Dispose();
        }
    }

    public record Model(string Name);

    public class Classic : IReducer
    {
        public Model Apply(PlainEvent @event, Model? current) => new(@event.Name);
    }
}
