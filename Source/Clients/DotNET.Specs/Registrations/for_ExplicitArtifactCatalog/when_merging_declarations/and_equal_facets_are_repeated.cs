// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_equal_facets_are_repeated : given.a_catalog
{
    void Establish() => _options
        .RegisterEventType<PlainEvent>(_ => _.WithId("same").WithId("same").Generation(2).Public().ForEventStore("current").PublishTo("target"))
        .RegisterEventType<PlainEvent>(_ => _.WithId("same").Generation(2).Public().ForEventStore("current").PublishTo("target"));

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_coalesce_membership() => _catalog.EventTypes.ShouldContainOnly(typeof(PlainEvent));
    [Fact] void should_coalesce_identity() => DescriptorFor(typeof(PlainEvent)).EventType.ShouldEqual(new EventType("same", 2));
    [Fact] void should_coalesce_ownership() => DescriptorFor(typeof(PlainEvent)).EventStore.ShouldEqual("current");
    [Fact] void should_coalesce_destination() => DescriptorFor(typeof(PlainEvent)).PublishingSequence.ShouldEqual((EventSequenceId)"target");
}
