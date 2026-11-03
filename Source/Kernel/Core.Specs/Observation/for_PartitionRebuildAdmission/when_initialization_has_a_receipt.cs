// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_PartitionRebuildAdmission;

public class when_initialization_has_a_receipt : given.an_admission_check
{
    void Establish() => _store.GenerationInitialization.GetReceipts().Returns(new[] { Receipt() });

    async Task Because() => _result = await _admission.IsAdmitted(_request);

    [Fact] void should_refuse_isolated_replay() => _result.ShouldBeFalse();
}
