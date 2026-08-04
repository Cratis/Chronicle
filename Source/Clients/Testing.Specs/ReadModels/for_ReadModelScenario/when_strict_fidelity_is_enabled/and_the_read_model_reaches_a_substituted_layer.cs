// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_strict_fidelity_is_enabled;

/// <summary>
/// The same seeding that passes in the default mode fails once a suite opts in to strict fidelity — the
/// ratchet. Where <see cref="ReadModelScenario{TReadModel}.Substitutions"/> informs, strict mode makes it
/// binding, so a newly added join cannot claim a green in-process spec before it is covered where the key
/// resolution is real.
/// </summary>
public class and_the_read_model_reaches_a_substituted_layer : Specification
{
    ReadModelScenario<JoinOrderSummary> _scenario;
    EventSourceId _orderId;
    Exception _error;

    void Establish()
    {
        _scenario = new ReadModelScenario<JoinOrderSummary>().WithStrictFidelity();
        _orderId = new EventSourceId(Guid.NewGuid());
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_orderId).Events(new JoinOrderPlaced(new JoinCustomerId(Guid.NewGuid()), 100m));
        _error = Catch.Exception(() => _ = _scenario.Instance);
    }

    [Fact] void should_reject_the_substituted_shape() => _error.ShouldBeOfExactType<ReadModelDependsOnSubstitutedLayer>();
    [Fact] void should_name_the_read_model() => _error.Message.ShouldContain(nameof(JoinOrderSummary));
    [Fact] void should_name_the_substituted_layer() => _error.Message.ShouldContain(nameof(ReadModelSubstitutedLayer.JoinKeyResolution));
    [Fact] void should_say_how_to_opt_out() => _error.Message.ShouldContain(nameof(ReadModelScenario<JoinOrderSummary>.WithStrictFidelity));
}
