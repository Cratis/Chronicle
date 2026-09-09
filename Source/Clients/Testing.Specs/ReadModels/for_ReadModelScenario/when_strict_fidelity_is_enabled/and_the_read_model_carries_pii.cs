// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_strict_fidelity_is_enabled;

/// <summary>
/// Strict fidelity used to hand a read model full of personal data a clean bill of health, because the
/// compliance layer was not among the ones it knew to look for. A false all-clear is worse than no check at
/// all on exactly this subject, so a read model whose schema carries compliance metadata now has to be
/// covered where compliance is real.
/// </summary>
public class and_the_read_model_carries_pii : Specification
{
    ReadModelScenario<PatientContactCard> _scenario;
    Exception _error;

    void Establish() => _scenario = new ReadModelScenario<PatientContactCard>().WithStrictFidelity();

    async Task Because()
    {
        await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new PatientAdmitted("grace@example.com"));
        _error = Catch.Exception(() => _ = _scenario.Instance);
    }

    [Fact] void should_reject_the_substituted_shape() => _error.ShouldBeOfExactType<ReadModelDependsOnSubstitutedLayer>();
    [Fact] void should_name_the_read_model() => _error.Message.ShouldContain(nameof(PatientContactCard));
    [Fact] void should_name_the_substituted_layer() => _error.Message.ShouldContain(nameof(ReadModelSubstitutedLayer.Compliance));
}
