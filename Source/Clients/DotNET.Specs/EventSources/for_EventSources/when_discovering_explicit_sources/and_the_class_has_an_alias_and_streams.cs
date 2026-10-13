// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.for_EventStore.when_preparing_for_registration.given;

namespace Cratis.Chronicle.EventSources.for_EventSources.when_discovering_explicit_sources;

public class and_the_class_has_an_alias_and_streams : an_explicit_store
{
    EventSourceDefinition _source;

    void Establish()
    {
        _options.ExplicitArtifacts.RegisterEventSource<PlainSource>("source-alias");
        _store = CreateStore();
    }

    async Task Because()
    {
        await _store.DiscoverAll();
        _source = _store.EventSources.GetFor(typeof(PlainSource));
    }

    [Fact] void should_use_the_registered_name() => _source.Name.ShouldEqual("source-alias");
    [Fact] void should_use_the_alias_as_the_source_type() => _source.EventSourceType.Value.ShouldEqual("source-alias");
    [Fact] void should_keep_declared_streams() => _source.Streams.Select(_ => _.Name).ShouldContainOnly("stream");
    [Fact] void should_leave_declaration_only_description_unchanged() => EventSources.Describe(typeof(PlainSource)).Name.ShouldEqual(nameof(PlainSource));

    [EventStream("stream")]
    public class PlainSource;
}
