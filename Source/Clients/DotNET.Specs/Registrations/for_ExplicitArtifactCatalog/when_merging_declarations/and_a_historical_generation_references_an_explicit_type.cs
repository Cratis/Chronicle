// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection.Emit;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Registrations.for_ExplicitArtifactCatalog.when_merging_declarations;

public class and_a_historical_generation_references_an_explicit_type : given.a_catalog
{
    Type _historical;

    void Establish()
    {
        var attribute = typeof(EventTypeGenerationForAttribute<>).MakeGenericType(typeof(PlainEvent));
        _historical = EmitType("HistoricalEvent", [new CustomAttributeBuilder(attribute.GetConstructor([typeof(uint)])!, [1u])]);
        _discovered.EventTypes.Returns([_historical]);
        _options.RegisterEventType<PlainEvent>(_ => _.WithId("shared").Generation(2));
    }

    void Because() => _catalog = _options.CreateSnapshot().ApplyTo(_discovered);

    [Fact] void should_resolve_the_referenced_explicit_identity() => DescriptorFor(_historical).EventType.ShouldEqual(new EventType("shared", 1));
    [Fact] void should_preserve_historical_exact_dispatch_exclusion() => _historical.GetEventTypes(_catalog.EventTypes).ShouldBeEmpty();
}
