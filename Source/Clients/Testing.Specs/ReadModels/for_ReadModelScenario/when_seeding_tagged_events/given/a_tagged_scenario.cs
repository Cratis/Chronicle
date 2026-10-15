// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_tagged_events.given;

public class a_tagged_scenario : Specification
{
    protected Defaults _defaults;
    protected ReadModelScenario<Balance> _scenario;
    protected EventSourceId _source;
    protected virtual Type ReducerType => typeof(Filtered);

    void Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<Credited>().RegisterEventType<UntaggedCredit>().RegisterEventSource<RoutedOrderSource>().RegisterReducer(ReducerType);
        _defaults = new(new InMemoryClientArtifactsProvider(), null, options);
        _scenario = new(null, _defaults);
        _source = EventSourceId.New();
    }

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }

    [Tag("static")]
    public record CreditBase(int Amount);

    [EventType]
    public record Credited(int Amount) : CreditBase(Amount);

    [EventType]
    public record UntaggedCredit(int Amount);

    public record Balance(int Amount, IReadOnlyList<string> Tags, EventSourceType SourceType, EventStreamType StreamType);

    public abstract class Counting
    {
        public Balance Credit(Credited @event, Balance? current, EventContext context) => Apply(@event.Amount, current, context);
        public Balance Credit(UntaggedCredit @event, Balance? current, EventContext context) => Apply(@event.Amount, current, context);

        static Balance Apply(int amount, Balance? current, EventContext context) => new((current?.Amount ?? 0) + amount, context.Tags.Select(tag => tag.Value).ToArray(), context.EventSourceType, context.EventStreamType);
    }

    [FilterEventsByTag("static"), FilterEventsByTag("dynamic")]
    public class Filtered : Counting;

    [FilterEventsByTag("dynamic"), EventSourceType("Orders"), EventStreamType("Payments")]
    public class Combined : Counting;

    [FilterEventsByTag("dynamic"), FromEventSource<RoutedOrderSource>("Payments")]
    public class Typed : Counting;

    [Tag("label"), Tags("dynamic")]
    public class Unfiltered : Counting;
}
