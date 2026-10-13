// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_event_facets_are_omitted : given.a_catalog
{
    Type _event;

    void Establish()
    {
        _event = EmitType("MarkedEvent",
        [
            AttributeFor<EventTypeAttribute>([typeof(string), typeof(uint)], "shared", 2u),
            AttributeFor<EventStoreAttribute>([typeof(string)], "foreign"),
            AttributeFor<PublicAttribute>([]),
            AttributeFor<PublishToAttribute>([typeof(string)], "destination")
        ]);
        _discovered.EventTypes.Returns([_event]);
        _options.RegisterEventType(_event).RegisterEventType(_event);
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_coalesce_the_clr_type() => _catalog.EventTypes.ShouldContainOnly(_event);
    [Fact] void should_preserve_the_marker_identity() => DescriptorFor(_event).EventType.ShouldEqual(_event.GetEventType());
    [Fact] void should_preserve_the_owner() => DescriptorFor(_event).EventStore.ShouldEqual("foreign");
    [Fact] void should_preserve_registration_visibility() => DescriptorFor(_event).RegistrationVisibility.ShouldEqual(Cratis.Chronicle.Contracts.Events.EventTypeVisibility.Public);
    [Fact] void should_preserve_public_eligibility() => DescriptorFor(_event).PublicationEligibility.ShouldBeTrue();
    [Fact] void should_preserve_the_destination() => DescriptorFor(_event).PublishingSequence.ShouldEqual((EventSequenceId)"destination");
}
