// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Observation.for_PartitionRebuildAdmission;

public class when_storage_cannot_be_read : given.an_admission_check
{
    void Establish() => _store.GenerationInitialization.GetReceipts().Returns(Task.FromException<IReadOnlyList<GenerationInitializationReceipt>>(new InvalidOperationException("Storage unavailable")));

    async Task Because() => _result = await _admission.IsAdmitted(_request);

    [Fact] void should_fail_closed() => _result.ShouldBeFalse();
}
