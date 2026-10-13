// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_private_is_supplied_for_an_owned_event : given.a_catalog
{
    Type _event;

    void Establish()
    {
        _event = EmitType("OwnedEvent", [AttributeFor<EventStoreAttribute>([typeof(string)], _store.Value)]);
        _options.RegisterEventType(_event, _ => _.Private());
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_register_privately() => DescriptorFor(_event).RegistrationVisibility.ShouldEqual(Cratis.Chronicle.Contracts.Events.EventTypeVisibility.Private);
    [Fact] void should_not_subtract_owned_publication_eligibility() => DescriptorFor(_event).PublicationEligibility.ShouldBeTrue();
}
