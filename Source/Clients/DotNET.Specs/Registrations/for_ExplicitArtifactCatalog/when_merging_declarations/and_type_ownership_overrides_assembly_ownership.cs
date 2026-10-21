// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_type_ownership_overrides_assembly_ownership : given.a_catalog
{
    Type _event;

    void Establish()
    {
        _event = EmitType("OwnedEvent",
            [AttributeFor<EventStoreAttribute>([typeof(string)], "type-owner")],
            [AttributeFor<EventStoreAttribute>([typeof(string)], "assembly-owner")]);
        _options.RegisterEventType(_event, _ => _.ForEventStore("type-owner"));
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_merge_only_the_effective_marker_owner() => DescriptorFor(_event).EventStore.ShouldEqual("type-owner");
    [Fact] void should_keep_foreign_registration_public() => DescriptorFor(_event).RegistrationVisibility.ShouldEqual(Cratis.Chronicle.Contracts.Events.EventTypeVisibility.Public);
    [Fact] void should_not_grant_foreign_publication() => DescriptorFor(_event).PublicationEligibility.ShouldBeFalse();
}
