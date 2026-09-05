// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using ContractCuts = Cratis.Chronicle.Contracts.Cuts;

namespace Cratis.Chronicle.Services.Cuts.for_ReadModelCuts.when_capturing;

public class and_the_read_model_does_not_exist : given.all_dependencies
{
    static readonly ReadModelIdentifier _unknownReadModel = "unknown-read-model";

    ContractCuts.ReadModelCutResponse _result;

    async Task Because() => _result = await _service.Capture(new ContractCuts.ReadModelCutRequest
    {
        EventStore = "my-event-store",
        Namespace = "my-namespace",
        Cuts = [new ContractCuts.EventSequenceCut { EventSequenceId = EventSequence, Position = 5UL }],
        Selection = [_unknownReadModel]
    });

    [Fact] void should_report_the_entry_as_not_found() => _result.Entries.Single().Outcome.ShouldEqual(ContractCuts.ReadModelCutOutcome.NotFound);
    [Fact] void should_not_save_any_payload() => _cutStorage.DidNotReceive().SavePayload(Arg.Any<Concepts.Cuts.ReadModelCutId>(), Arg.Any<ReadModelIdentifier>(), Arg.Any<string>());
}
