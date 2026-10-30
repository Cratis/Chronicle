// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads;

public class when_reading_from_a_kernel_without_a_mutation_epoch : given.a_decision_reader
{
    DecisionRead<Model> _read;

    async Task Because() => _read = await _reader.GetDetached<Model>("source");

    [Fact] void should_not_carry_an_epoch() => ((IDecisionRead)_read).Scope.MutationEpoch.ShouldBeNull();
}
