// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_EventAppendCollection.given;

public class a_reactor_that_directly_appends_unique_events_scope(ChronicleFixture fixture) : Specification(fixture)
{
    public EventSourceId EventSourceId;
    public IEventAppendCollection AppendedEventsCollector;

    public override IEnumerable<Type> EventTypes => [typeof(ADirectUniqueEvent), typeof(ADirectUniqueFollowUpEvent)];
    public override IEnumerable<Type> ConstraintTypes => [typeof(ADirectUniqueFollowUpConstraint)];
    public override IEnumerable<Type> Reactors => [typeof(AReactorThatDirectlyAppendsUniqueEvent)];

    protected override void ConfigureServices(IServiceCollection services) =>
        services.AddSingleton<AReactorThatDirectlyAppendsUniqueEvent>();

    void Establish() => EventSourceId = EventSourceId.New();

    void Destroy() => AppendedEventsCollector?.Dispose();
}
