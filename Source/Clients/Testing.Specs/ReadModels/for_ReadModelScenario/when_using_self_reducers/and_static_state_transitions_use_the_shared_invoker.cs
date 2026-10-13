// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_using_self_reducers;

public class and_static_state_transitions_use_the_shared_invoker : Specification
{
    Defaults _defaults;
    ReadModelScenario<StaticBalance> _scenario;

    void Establish()
    {
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterEventType<Credit>().RegisterReducer<StaticBalance>();
        _defaults = new(new InMemoryClientArtifactsProvider(), null, options);
        _scenario = new(null, _defaults);
    }

    async Task Because()
    {
        var source = EventSourceId.New();
        await _scenario.Given.ForEventSource(source).WithTags("red").Events(new Credit(100));
        await _scenario.Given.ForEventSource(source).WithTags("accepted").Events(new Credit(3), new Credit(4), new Credit(-1), new Credit(2));
    }

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }

    [Fact] void should_create_delete_and_recreate_without_model_activation() => _scenario.Instance!.Amount.ShouldEqual(2);
    [Fact] void should_keep_reducer_tag_selection() => _scenario.Instance!.Tags.ShouldContainOnly("accepted");

    public record Credit(int Amount);

    [FilterEventsByTag("accepted")]
    public record StaticBalance(int Amount, IReadOnlyList<string> Tags)
    {
        public static Task<StaticBalance?> Reduce(Credit @event, StaticBalance? current, EventContext context) =>
            Task.FromResult(@event.Amount == -1 ? null : new StaticBalance((current?.Amount ?? 0) + @event.Amount, context.Tags.Select(tag => tag.Value).ToArray()));
    }
}
