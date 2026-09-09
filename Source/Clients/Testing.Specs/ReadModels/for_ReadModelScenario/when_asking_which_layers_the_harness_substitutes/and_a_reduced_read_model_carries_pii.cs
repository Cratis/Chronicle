// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asking_which_layers_the_harness_substitutes;

public class and_a_reduced_read_model_carries_pii : Specification
{
    ReadModelScenario<ReducedPatientContact> _scenario;
    IReadOnlyList<ReadModelSubstitution> _substitutions;

    void Establish() => _scenario = new ReadModelScenario<ReducedPatientContact>();

    async Task Because()
    {
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new PatientAdmitted("patient@example.com"));
        _substitutions = _scenario.Substitutions;
    }

    [Fact] void should_report_substituted_compliance() => _substitutions.Single().Layer.ShouldEqual(ReadModelSubstitutedLayer.Compliance);
    [Fact] void should_still_reduce_plaintext() => _scenario.Instance!.EmailAddress.Value.ShouldEqual("patient@example.com");
}
