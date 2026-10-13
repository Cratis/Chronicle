// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_nominated_constraints_are_already_listed : given.a_catalog
{
    Type _unique;
    Type _closing;
    Type _removal;

    void Establish()
    {
        _unique = EmitType("UniqueNominated", [AttributeFor<UniqueAttribute>([typeof(string), typeof(string)], "unique", "unique")]);
        _closing = EmitType("ClosingNominated", [AttributeFor<ClosesStreamAttribute>([])]);
        _removal = EmitType("RemovalNominated", [AttributeFor<RemoveConstraintAttribute>([typeof(string)], "unique")]);
        _discovered.EventTypes.Returns([_unique, _closing, _removal, typeof(PropertyUnique)]);
        _discovered.UniqueEventTypeConstraints.Returns([_unique]);
        _discovered.UniqueConstraints.Returns([typeof(PropertyUnique)]);
        _discovered.RemoveConstraintEventTypes.Returns([_removal]);
        _discovered.ClosesStreamEventTypes.Returns([_closing]);
        _options.RegisterEventType(_unique).RegisterEventType(_closing).RegisterEventType(_removal).RegisterEventType<PropertyUnique>();
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_coalesce_class_constraints() => _catalog.UniqueEventTypeConstraints.ShouldContainOnly(_unique);
    [Fact] void should_coalesce_property_constraints() => _catalog.UniqueConstraints.ShouldContainOnly(typeof(PropertyUnique));
    [Fact] void should_coalesce_removals() => _catalog.RemoveConstraintEventTypes.ShouldContainOnly(_removal);
    [Fact] void should_coalesce_stream_closures() => _catalog.ClosesStreamEventTypes.ShouldContainOnly(_closing);

    public record PropertyUnique([property: Unique] string Value);
}
