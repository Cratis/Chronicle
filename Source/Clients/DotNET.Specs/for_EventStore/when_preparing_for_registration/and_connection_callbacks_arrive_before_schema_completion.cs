// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.EventStores;

namespace Cratis.Chronicle.for_EventStore.when_preparing_for_registration;

public class and_connection_callbacks_arrive_before_schema_completion : given.an_explicit_store
{
    TaskCompletionSource _schemaEntered;
    TaskCompletionSource _finishSchema;
    volatile bool _schemaFinished;
    bool _identitiesAvailableBeforeSchemas;
    int _earlyEffects;

    void Establish()
    {
        _auto = true;
        _schemaEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _finishSchema = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _options.ExplicitArtifacts.RegisterEventType<PlainEvent>();
        _generator.BeforeGenerate = (_, _, _) =>
        {
            _schemaEntered.TrySetResult();
            _finishSchema.Task.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            _schemaFinished = true;
        };
        _services.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(_ =>
        {
            if (!_schemaFinished) _earlyEffects++;
            return CommandResult.Success(Guid.NewGuid());
        });
        _store = CreateStore();
    }

    async Task Because()
    {
        var discovery = Task.Run(_store.DiscoverAll);
        await _schemaEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _identitiesAvailableBeforeSchemas = _store.EventTypes.HasFor(typeof(PlainEvent));
        var connected = _connection.Lifecycle.Connected();
        _finishSchema.SetResult();
        await Task.WhenAll(discovery, connected).WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_make_identities_available_for_internal_schema_binding() => _identitiesAvailableBeforeSchemas.ShouldBeTrue();
    [Fact] void should_not_send_effects_on_identity_availability_alone() => _earlyEffects.ShouldEqual(0);
    [Fact] void should_complete_registration_after_the_schema_verdict() => _store.Registration.IsSuccess.ShouldBeTrue();
}
