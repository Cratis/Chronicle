// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using ContractCuts = Cratis.Chronicle.Contracts.Cuts;

namespace Cratis.Chronicle.Services.Cuts.for_ReadModelCuts.when_capturing;

public class and_no_cut_is_provided_for_the_projections_event_sequence : given.all_dependencies
{
    ContractCuts.ReadModelCutResponse _result;

    async Task Because() => _result = await _service.Capture(new ContractCuts.ReadModelCutRequest
    {
        EventStore = "my-event-store",
        Namespace = "my-namespace",
        Cuts = [new ContractCuts.EventSequenceCut { EventSequenceId = "some-other-sequence", Position = 5UL }],
        Selection = [ReadModel]
    });

    [Fact] void should_report_the_entry_as_failed() => _result.Entries.Single().Outcome.ShouldEqual(ContractCuts.ReadModelCutOutcome.Failed);
    [Fact] void should_explain_the_failure() => _result.Entries.Single().FailureReason.ShouldNotBeNull();
    [Fact] void should_not_save_any_payload() => _cutStorage.DidNotReceive().SavePayload(Arg.Any<Concepts.Cuts.ReadModelCutId>(), Arg.Any<ReadModelIdentifier>(), Arg.Any<string>());
}
