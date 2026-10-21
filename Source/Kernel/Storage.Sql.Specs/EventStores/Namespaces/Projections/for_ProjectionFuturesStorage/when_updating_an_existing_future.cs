// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Projections.for_ProjectionFuturesStorage;

public class when_updating_an_existing_future : given.a_futures_storage
{
    EventContext _read;

    async Task Establish() => await _storage.Save(_future.ProjectionId, _future with
    {
        Event = _future.Event with { Context = EventContext.Empty }
    });

    async Task Because()
    {
        await _storage.Save(_future.ProjectionId, _future);
        _read = (await _storage.GetForProjection(_future.ProjectionId)).Single().Event.Context;
    }

    [Fact] void should_replace_the_occurred_time() => _read.Occurred.ShouldEqual(DateTimeOffset.Parse("2026-05-12T13:14:15.1234567+02:00"));
    [Fact] void should_replace_the_correlation_id() => _read.CorrelationId.ShouldEqual(new CorrelationId(Guid.Parse("de001d50-d624-4d65-a550-314241d40e07")));
    [Fact] void should_replace_the_sequence_number() => _read.SequenceNumber.ShouldEqual(new EventSequenceNumber(42));
}
