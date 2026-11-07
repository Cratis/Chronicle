// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Tagging.for_EventTagResolver.when_resolving;

public class with_discovery_yielding_null_and_built_in_enrichers : given.an_event_tag_resolver
{
    IReadOnlyList<NamedTag> _result;

    void Establish()
    {
        _existing = [Tag("call", "site")];

        // Discovery can yield null for a type the service provider cannot create, and it also finds the built-in enrichers.
        _resolver = new EventTagResolver([null!, new AmbientEventTagEnricher(), _enricher]);
        _enricher.GetTags(Arg.Any<EventTagContext>()).Returns([Tag("enriched", "yes")]);
    }

    void Because() => _result = Resolve(new PlainEvent("name"));

    [Fact] void should_not_fail() => _result.ShouldNotBeNull();
    [Fact] void should_keep_existing_and_enriched_tags() => _result.Select(_ => (_.Name.Value, _.Value)).ShouldEqual([("call", "site"), ("enriched", "yes")]);
}
