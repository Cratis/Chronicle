// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_activated_with_an_unsupported_sequence : given.a_subscriber_with_a_cached_pipeline
{
    protected override ProjectionDefinition DefinitionAtActivation() =>
        _definition with { Owner = ProjectionOwner.Server, EventSequenceId = " SyStEm " };

    void Because()
    {
    }

    [Fact] void should_not_return_a_pipeline() => _returnedPipeline.ShouldBeNull();
}
