// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_publication_is_inherited : given.a_catalog
{
    Type _event;
    EventTypeDescriptor _legacy;

    void Establish()
    {
        var parent = EmitType("Parent", [AttributeFor<PublicAttribute>([])]);
        _event = EmitType("Child", [AttributeFor<EventTypeAttribute>([typeof(string), typeof(uint)], "child", 1u)], baseType: parent);
        _legacy = EventTypeDescriptor.FromDeclaration(_event, _store);
        _discovered.EventTypes.Returns([_event]);
        _options.RegisterEventType(_event, _ => _.Private());
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_preserve_the_distinct_legacy_visibility_rule() => DescriptorFor(_event).RegistrationVisibility.ShouldEqual(_legacy.RegistrationVisibility);
    [Fact] void should_preserve_inherited_publication_eligibility() => DescriptorFor(_event).PublicationEligibility.ShouldEqual(_legacy.PublicationEligibility);
    [Fact] void should_not_subtract_inherited_publication() => DescriptorFor(_event).PublicationEligibility.ShouldBeTrue();
}
