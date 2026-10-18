// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key;

public class without_a_handler_and_with_strict_fidelity : Specification
{
    ReadModelScenario<LookupOnlySeedWithId> _scenario;
    Exception _error;

    void Establish()
    {
        _scenario = new ReadModelScenario<LookupOnlySeedWithId>().WithStrictFidelity();
        _scenario.Given.ForEventSourceId("invoice-1").ReadModel(new("invoice-1", "lookup only"));
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId("invoice-1").Events(new SeededKeyRenamed("after"));
        _error = Catch.Exception(() => _ = _scenario.Instance);
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_have_a_substituted_layer() => _scenario.Substitutions.ShouldNotBeEmpty();
    [Fact] void should_reject_the_substituted_layer() => _error.ShouldBeOfExactType<ReadModelDependsOnSubstitutedLayer>();
}
