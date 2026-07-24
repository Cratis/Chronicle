// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_ProjectionFutures.given;

public class the_futures_grain : Specification
{
    protected TestKitSilo _silo = new();
    protected ProjectionFutures _grain;
    protected IStorage<ProjectionFuturesState> _stateStorage;

    async Task Establish()
    {
        _stateStorage = _silo.StorageManager.GetStorage<ProjectionFuturesState>(typeof(ProjectionFutures).FullName!);
        _grain = await _silo.CreateGrainAsync<ProjectionFutures>("test-projection");
    }

    protected static ProjectionFuture CreateFuture(EventSequenceNumber sequenceNumber) => new(
        new ProjectionFutureId(Guid.NewGuid()),
        new ProjectionId("test-projection"),
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(new EventType("TestEvent", EventTypeGeneration.First), sequenceNumber),
        PropertyPath.NotSet,
        new PropertyPath("Children"),
        PropertyPath.NotSet,
        PropertyPath.NotSet,
        new Key("parent-key", ArrayIndexers.NoIndexers),
        DateTimeOffset.UtcNow);
}
