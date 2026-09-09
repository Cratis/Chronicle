// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_asking_which_layers_the_harness_substitutes;

/// <summary>
/// The value a running system stores as ciphertext is projected and read as plaintext here, so a spec
/// asserting on it looks exactly as green whether the <c>[PII]</c> marker is honored or ignored. That is the
/// one substitution whose absence from the report is indistinguishable from success, so the report has to
/// name it.
/// </summary>
public class and_the_read_model_carries_pii : Specification
{
    ReadModelScenario<PatientContactCard> _scenario;
    IReadOnlyList<ReadModelSubstitution> _substitutions;

    void Establish() => _scenario = new ReadModelScenario<PatientContactCard>();

    async Task Because()
    {
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new PatientAdmitted("ada@example.com"));
        _substitutions = _scenario.Substitutions;
    }

    [Fact] void should_report_compliance() => _substitutions.Single().Layer.ShouldEqual(ReadModelSubstitutedLayer.Compliance);
    [Fact] void should_still_project_the_read_model() => _scenario.Instance!.EmailAddress.Value.ShouldEqual("ada@example.com");
}
