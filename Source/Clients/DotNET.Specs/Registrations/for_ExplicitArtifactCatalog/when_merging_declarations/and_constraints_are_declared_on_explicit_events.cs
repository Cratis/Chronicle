// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_constraints_are_declared_on_explicit_events : given.a_catalog
{
    Type _closing;
    Type _removal;

    void Establish()
    {
        _closing = EmitType("Closing", [AttributeFor<ClosesStreamAttribute>([])]);
        _removal = EmitType("Removal", [AttributeFor<RemoveConstraintAttribute>([typeof(string)], "unique")]);
        _options.RegisterEventType<PropertyUnique>().RegisterEventType(_closing).RegisterEventType(_removal);
        _discovered.UniqueConstraints.Returns([typeof(PlainEvent)]);
        _discovered.UniqueEventTypeConstraints.Returns([typeof(OtherEvent)]);
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_include_property_constraints_without_erasing_custom_lists() => _catalog.UniqueConstraints.ShouldContainOnly(typeof(PlainEvent), typeof(PropertyUnique));
    [Fact] void should_preserve_custom_class_constraints() => _catalog.UniqueEventTypeConstraints.ShouldContainOnly(typeof(OtherEvent));
    [Fact] void should_include_closing_declarations() => _catalog.ClosesStreamEventTypes.ShouldContainOnly(_closing);
    [Fact] void should_include_constraint_removals() => _catalog.RemoveConstraintEventTypes.ShouldContainOnly(_removal);

    public record PropertyUnique([property: Unique] string Value);
}
