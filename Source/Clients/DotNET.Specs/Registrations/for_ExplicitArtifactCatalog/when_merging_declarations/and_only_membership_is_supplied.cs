// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_only_membership_is_supplied : given.a_catalog
{
    void Establish() => _options.RegisterEventType<PlainEvent>();

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_derive_the_existing_default_identity() => DescriptorFor(typeof(PlainEvent)).EventType.ShouldEqual(ArtifactIdentity.EventTypeFor(typeof(PlainEvent)));
    [Fact] void should_default_to_private_registration() => DescriptorFor(typeof(PlainEvent)).RegistrationVisibility.ShouldEqual(Cratis.Chronicle.Contracts.Events.EventTypeVisibility.Private);
    [Fact] void should_not_grant_publication() => DescriptorFor(typeof(PlainEvent)).PublicationEligibility.ShouldBeFalse();
    [Fact] void should_default_to_the_outbox() => DescriptorFor(typeof(PlainEvent)).PublishingSequence.ShouldEqual(EventSequenceId.Outbox);
    [Fact] void should_leave_declaration_only_helpers_unchanged() => Catch.Exception(() => typeof(PlainEvent).GetEventType()).ShouldBeOfExactType<TypeIsNotAnEventType>();
    [Fact] void should_not_mutate_the_provider() => _discovered.EventTypes.ShouldBeEmpty();
}
