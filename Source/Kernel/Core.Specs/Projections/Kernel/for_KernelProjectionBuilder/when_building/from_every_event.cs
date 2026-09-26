// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Kernel.given;

namespace Cratis.Chronicle.Projections.Kernel.for_KernelProjectionBuilder.when_building;

public class from_every_event : Specification
{
    KernelProjectionBuilder<a_read_model> _builder;
    ProjectionDefinition _result;

    void Establish()
    {
        _builder = new(WellKnownKernelProjections.IdentifierFor("event-count"), "event-count");
        _builder.FromEvery(every => every.Count(model => model.Count));
    }

    void Because() => _result = _builder.Build().Single();

    [Fact] void should_subscribe_to_all_events() => _result.SubscribesToAllEvents.ShouldBeTrue();
    [Fact] void should_count_into_the_count_property() => _result.FromEvery.Properties[new("Count")].ShouldEqual(WellKnownExpressions.Count);
}
