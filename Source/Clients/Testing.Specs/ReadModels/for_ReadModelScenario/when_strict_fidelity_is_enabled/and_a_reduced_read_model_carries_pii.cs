// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_strict_fidelity_is_enabled;

public class and_a_reduced_read_model_carries_pii : Specification
{
    ReadModelScenario<ReducedPatientContact> _scenario;
    Exception _error;

    void Establish() => _scenario = new ReadModelScenario<ReducedPatientContact>().WithStrictFidelity();

    async Task Because()
    {
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new PatientAdmitted("patient@example.com"));
        _error = Catch.Exception(() => _ = _scenario.Instance);
    }

    [Fact] void should_reject_substituted_compliance() => _error.ShouldBeOfExactType<ReadModelDependsOnSubstitutedLayer>();
    [Fact] void should_identify_compliance() => _error.Message.ShouldContain(nameof(ReadModelSubstitutedLayer.Compliance));
    [Fact] void should_identify_the_read_model() => _error.Message.ShouldContain(nameof(ReducedPatientContact));
}
