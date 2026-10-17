// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;

namespace Cratis.Chronicle.Testing.Occurrences.for_DropReplyInterceptor.given;

public class an_occurrence : for_OccurrenceScenario.given.a_scenario
{
    protected IOccurrences _wire;
    protected ClaimOccurrencesRequest _claim;

    void Establish()
    {
        _wire = ((IChronicleServicesAccessor)_scenario.EventStore.Connection).Services.Occurrences;
        _claim = new() { EventStore = _scenario.EventStore.Name, Namespace = _scenario.EventStore.Namespace, Trigger = "testing-import", WorkerId = "test-worker", Max = 1, LeaseSeconds = 60 };
    }

    protected async Task<OccurrenceClaimDetails> Claim() => (await _wire.ClaimOccurrences(_claim).EnsureSuccess()).Claims.Single();
}
