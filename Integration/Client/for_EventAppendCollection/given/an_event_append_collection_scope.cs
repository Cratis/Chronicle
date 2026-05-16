// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventAppendCollection.given;

public class an_event_append_collection_scope(ChronicleFixture fixture) : Specification(fixture)
{
    public EventSourceId EventSourceId;
    public IEventAppendCollection AppendedEventsCollector;

    public override IEnumerable<Type> EventTypes => [typeof(AnEventHappened), typeof(AnotherEventHappened)];
    public override IEnumerable<Type> Reactors => [typeof(AReactor)];

    protected override void ConfigureServices(IServiceCollection services) =>
        services.AddSingleton(new AReactor());

    void Establish() => EventSourceId = EventSourceId.New();

    void Destroy() => AppendedEventsCollector?.Dispose();
}
