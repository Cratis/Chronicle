// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_an_unknown_refusal_remains : given.an_initializer
{
    async Task Establish() => await _audit.RecordBlocker(new(_name, GenerationInitializationBlockerKind.ContradictoryLegacyState, "unrecognized-subject", "operator reconciliation required", DateTimeOffset.UtcNow, null));

    async Task Because() => _result = await _initializer.Initialize(_name);

    [Fact] void should_not_ignore_durable_refusal() => _result.ShouldBeOfExactType<GenerationInitializationBlocked>();
    [Fact] async Task should_not_write_an_enabled_marker() => (await _audit.GetReceipts()).Any(receipt => receipt.Step == GenerationInitializationStep.Enabled).ShouldBeFalse();
}
