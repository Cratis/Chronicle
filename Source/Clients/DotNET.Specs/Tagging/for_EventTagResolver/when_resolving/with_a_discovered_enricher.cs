// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Tagging.for_EventTagResolver.when_resolving;

public class with_a_discovered_enricher : given.an_event_tag_resolver
{
    IReadOnlyList<NamedTag> _result;
    PlainEvent _event = new("name");

    void Establish()
    {
        _existing = [Tag("call", "site"), Tag("shared", "same")];
        _enricher.GetTags(Arg.Any<EventTagContext>()).Returns([Tag("shared", "same"), Tag("enriched", "yes")]);
    }

    void Because() => _result = Resolve(_event);

    [Fact] void should_ask_the_enricher_with_the_event() => _enricher.Received(1).GetTags(Arg.Is<EventTagContext>(_ => ReferenceEquals(_.Event, _event) && _.EventStoreName.Value == "Operator"));
    [Fact] void should_give_the_enricher_the_existing_tags() => _enricher.Received(1).GetTags(Arg.Is<EventTagContext>(_ => _.ExistingTags.Count == 2));
    [Fact] void should_keep_existing_tags_first_and_add_enriched_without_duplicates() => _result.Select(_ => (_.Name.Value, _.Value)).ShouldEqual([("call", "site"), ("shared", "same"), ("enriched", "yes")]);
}
