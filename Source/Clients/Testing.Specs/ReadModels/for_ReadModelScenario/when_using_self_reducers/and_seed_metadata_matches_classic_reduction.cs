// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_using_self_reducers;

public class and_seed_metadata_matches_classic_reduction : Specification
{
    IState? _classic;
    IState? _self;
    IState? _excludedClassic;
    IState? _excludedSelf;

    async Task Because()
    {
        _classic = await Run<ClassicState>(typeof(Classic), accepted: true);
        _self = await Run<SelfState>(typeof(SelfState), accepted: true);
        _excludedClassic = await Run<ClassicState>(typeof(Classic), accepted: false);
        _excludedSelf = await Run<SelfState>(typeof(SelfState), accepted: false);
    }

    static async Task<T?> Run<T>(Type reducer, bool accepted)
        where T : class
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<Credit>().RegisterEventType<OtherCredit>().RegisterEventSource<RoutedOrderSource>().RegisterReducer(reducer);
        var defaults = new Defaults(new InMemoryClientArtifactsProvider(), null, options);
        using var scenario = new ReadModelScenario<T>(null, defaults);
        try
        {
            var source = EventSourceId.New();
            var tags = new[] { "append" };
            var builder = scenario.Given.ForEventSource(source).WithTags(tags);
            tags[0] = "mutated";
            await builder.OnStream("Other", "Payments").Events(new Credit(100));
            await scenario.Given.ForEventSource(source).OnStream("RoutedOrder", "Other").WithTags("append").Events(new Credit(100));
            await scenario.Given.ForEventSource(source).OnStream("RoutedOrder", "Payments").WithTags("label").Events(new OtherCredit(100));
            if (accepted)
            {
                await builder.OnStream("RoutedOrder", "Payments").Events(new Credit(3));
                await scenario.Given.ForEventSource(source).OnStream("RoutedOrder", "Payments").WithTags("append").Events(new OtherCredit(2));
            }
            return scenario.Instance;
        }
        finally
        {
            ((IDisposable)defaults.EventStore).Dispose();
        }
    }

    [Fact] void should_reduce_only_the_accepted_history() => _self!.Amount.ShouldEqual(5);
    [Fact] void should_match_the_classic_golden_result() => _self!.Amount.ShouldEqual(_classic!.Amount);
    [Fact] void should_capture_the_builder_tags_instead_of_mutations() => _self!.SeenTags.ShouldEqual("append,inherited,static|append");
    [Fact] void should_match_delivered_classic_tags() => _self!.SeenTags.ShouldEqual(_classic!.SeenTags);
    [Fact] void should_preserve_typed_source_metadata() => _self!.Source.Value.ShouldEqual("RoutedOrder");
    [Fact] void should_preserve_stream_metadata() => _self!.Stream.Value.ShouldEqual("Payments");
    [Fact] void should_keep_an_all_excluded_self_history_absent() => _excludedSelf.ShouldBeNull();
    [Fact] void should_keep_an_all_excluded_classic_history_absent() => _excludedClassic.ShouldBeNull();

    public interface IState
    {
        int Amount { get; }
        string SeenTags { get; }
        EventSourceType Source { get; }
        EventStreamType Stream { get; }
    }
    [Tag("inherited")] public record CreditMetadata;
    [Tag("static")] public record Credit(int Amount) : CreditMetadata;
    public record OtherCredit(int Amount);
    public record ClassicState(int Amount, string SeenTags, EventSourceType Source, EventStreamType Stream) : IState;
    [FilterEventsByTag("static"), FilterEventsByTag("append"), Tag("label")]
    public record ObserverMetadata;
    [FromEventSource<RoutedOrderSource>("Payments")]
    public record Classic : ObserverMetadata, IReducerFor<ClassicState>
    {
        public ClassicState Apply(Credit @event, ClassicState? current, EventContext context) => Fold(@event.Amount, current, context);
        public ClassicState Apply(OtherCredit @event, ClassicState? current, EventContext context) => Fold(@event.Amount, current, context);

        static ClassicState Fold(int amount, ClassicState? current, EventContext context) => new((current?.Amount ?? 0) + amount, TagsFor(current?.SeenTags, context), context.EventSourceType, context.EventStreamType);
    }
    [FromEventSource<RoutedOrderSource>("Payments")]
    public record SelfState(int Amount, string SeenTags, EventSourceType Source, EventStreamType Stream) : ObserverMetadata, IState
    {
        public static Task<SelfState> Reduce(Credit @event, SelfState? current, EventContext context) => Task.FromResult(Fold(@event.Amount, current, context));
        public static SelfState Reduce(OtherCredit @event, SelfState? current, EventContext context) => Fold(@event.Amount, current, context);

        static SelfState Fold(int amount, SelfState? current, EventContext context) => new((current?.Amount ?? 0) + amount, TagsFor(current?.SeenTags, context), context.EventSourceType, context.EventStreamType);
    }

    static string TagsFor(string? previous, EventContext context) => $"{(previous is null ? string.Empty : $"{previous}|")}{string.Join(',', context.Tags.Select(tag => tag.Value).Order(StringComparer.Ordinal))}";
}
