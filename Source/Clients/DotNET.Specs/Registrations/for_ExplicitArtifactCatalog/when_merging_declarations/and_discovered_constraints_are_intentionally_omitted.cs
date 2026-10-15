// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_discovered_constraints_are_intentionally_omitted : given.a_catalog
{
    void Establish()
    {
        var unique = EmitType("UniqueDiscovered", [AttributeFor<UniqueAttribute>([typeof(string), typeof(string)], "unique", "unique")]);
        var closing = EmitType("ClosingDiscovered", [AttributeFor<ClosesStreamAttribute>([])]);
        var removal = EmitType("RemovalDiscovered", [AttributeFor<RemoveConstraintAttribute>([typeof(string)], "unique")]);
        _discovered.EventTypes.Returns([unique, closing, removal, typeof(PropertyUnique)]);
        _options.RegisterEventType<PlainEvent>();
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_honor_omitted_class_constraints() => _catalog.UniqueEventTypeConstraints.ShouldBeEmpty();
    [Fact] void should_honor_omitted_property_constraints() => _catalog.UniqueConstraints.ShouldBeEmpty();
    [Fact] void should_honor_omitted_removals() => _catalog.RemoveConstraintEventTypes.ShouldBeEmpty();
    [Fact] void should_honor_omitted_stream_closures() => _catalog.ClosesStreamEventTypes.ShouldBeEmpty();

    public record PropertyUnique([property: Unique] string Value);
}
