// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_two_generations_share_an_id : given.a_catalog
{
    void Establish() => _options
        .RegisterEventType<PlainEvent>(_ => _.WithId("same"))
        .RegisterEventType<OtherEvent>(_ => _.WithId("same").Generation(2));

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_keep_both_generations() => _catalog.EventTypes.ShouldContainOnly(typeof(PlainEvent), typeof(OtherEvent));
    [Fact] void should_resolve_the_first_identity() => DescriptorFor(typeof(PlainEvent)).EventType.ShouldEqual(new EventType("same", 1));
    [Fact] void should_resolve_the_second_identity() => DescriptorFor(typeof(OtherEvent)).EventType.ShouldEqual(new EventType("same", 2));
    [Fact] void should_not_make_a_numeric_generation_historical() => typeof(OtherEvent).GetEventTypes(_catalog.EventTypes).ShouldContainOnly(typeof(OtherEvent));
}
