// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_observer_and_source_ids_are_omitted : given.a_catalog
{
    Type _reactor;
    Type _reducer;
    Type _source;

    void Establish()
    {
        _reactor = EmitType("MarkerReactor", [AttributeFor<ReactorAttribute>([typeof(string), typeof(string)], "reactor-id", null!)], interfaces: [typeof(IReactor)]);
        _reducer = EmitType("MarkerReducer", [AttributeFor<ReducerAttribute>([typeof(string), typeof(string), typeof(bool)], "reducer-id", null!, true)]);
        _source = EmitType("MarkerEventSource", [AttributeFor<EventSourceAttribute>([typeof(string)], "source-id")]);
        _discovered.Reactors.Returns([_reactor]);
        _discovered.Reducers.Returns([_reducer]);
        _discovered.EventSources.Returns([_source]);
        _options.RegisterReactor(_reactor).RegisterReducer(_reducer).RegisterEventSource(_source);
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_preserve_the_reactor_id() => ExplicitArtifactCatalog.From(_catalog)!.GetReactorIdFor(_reactor).ShouldEqual((ReactorId)"reactor-id");
    [Fact] void should_preserve_the_reducer_id() => ExplicitArtifactCatalog.From(_catalog)!.GetReducerIdFor(_reducer).ShouldEqual((ReducerId)"reducer-id");
    [Fact] void should_preserve_the_source_name() => ExplicitArtifactCatalog.From(_catalog)!.GetEventSourceNameFor(_source).ShouldEqual("source-id");
    [Fact] void should_coalesce_reactors() => _catalog.Reactors.ShouldContainOnly(_reactor);
    [Fact] void should_coalesce_reducers() => _catalog.Reducers.ShouldContainOnly(_reducer);
    [Fact] void should_coalesce_sources() => _catalog.EventSources.ShouldContainOnly(_source);
}
