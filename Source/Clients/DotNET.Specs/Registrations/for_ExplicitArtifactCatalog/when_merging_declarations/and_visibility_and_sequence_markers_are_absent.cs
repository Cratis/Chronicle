// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_visibility_and_sequence_markers_are_absent : given.a_catalog
{
    Type _event;

    void Establish()
    {
        _event = EmitType("MarkedEvent", [AttributeFor<EventTypeAttribute>([typeof(string), typeof(uint)], "", 1u)]);
        _discovered.EventTypes.Returns([_event]);
        _options.RegisterEventType(_event, _ => _.Public().PublishTo("destination"));
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_accept_public_as_a_supplied_facet_not_an_override() => DescriptorFor(_event).RegistrationVisibility.ShouldEqual(Cratis.Chronicle.Contracts.Events.EventTypeVisibility.Public);
    [Fact] void should_grant_publication_eligibility() => DescriptorFor(_event).PublicationEligibility.ShouldBeTrue();
    [Fact] void should_accept_a_nondefault_destination() => DescriptorFor(_event).PublishingSequence.ShouldEqual((EventSequenceId)"destination");
}
