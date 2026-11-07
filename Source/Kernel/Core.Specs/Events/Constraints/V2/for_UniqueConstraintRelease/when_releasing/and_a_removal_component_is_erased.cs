// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintRelease.when_releasing;

public class and_a_removal_component_is_erased : given.a_factory
{
    void Establish()
    {
        var constraint = new UniqueConstraintDefinition("number", [new("claimed", ["number"])])
        {
            RemovalEventDefinitions = [new("event", ["number"])]
        };
        _definition = constraint.RemovalEventDefinitions.Single();
        _statuses["number"] = ProtectedValueReleaseStatus.Erased;
    }

    async Task Because() => _result = await Create();

    [Fact] void should_report_nothing_to_release_for_erasure() => _result.Outcome.ShouldEqual(UniqueConstraintReleaseOutcome.NoClaimErased);
    [Fact] void should_not_produce_components_to_hash_for_removal() => _result.Components.Count.ShouldEqual(0);
    [Fact] void should_preserve_the_original_ciphertext() => CurrentContent().ShouldEqual(_original);
}
