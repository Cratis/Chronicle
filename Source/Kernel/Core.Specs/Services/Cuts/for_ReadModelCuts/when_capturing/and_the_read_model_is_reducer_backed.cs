// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using ContractCuts = Cratis.Chronicle.Contracts.Cuts;

namespace Cratis.Chronicle.Services.Cuts.for_ReadModelCuts.when_capturing;

public class and_the_read_model_is_reducer_backed : given.all_dependencies
{
    ContractCuts.ReadModelCutResponse _result;

    void Establish()
    {
        _readModelDefinition = _readModelDefinition with { ObserverType = ReadModelObserverType.Reducer };
        _readModelDefinitionsStorage.GetAll().Returns([_readModelDefinition]);
    }

    async Task Because() => _result = await _service.Capture(new ContractCuts.ReadModelCutRequest
    {
        EventStore = "my-event-store",
        Namespace = "my-namespace",
        Cuts = [new ContractCuts.EventSequenceCut { EventSequenceId = EventSequence, Position = 5UL }],
        Selection = [ReadModel]
    });

    [Fact] void should_report_the_entry_as_unsupported() => _result.Entries.Single().Outcome.ShouldEqual(ContractCuts.ReadModelCutOutcome.Unsupported);
    [Fact] void should_not_save_any_payload() => _cutStorage.DidNotReceive().SavePayload(Arg.Any<Concepts.Cuts.ReadModelCutId>(), Arg.Any<ReadModelIdentifier>(), Arg.Any<string>());
    [Fact] void should_still_publish_the_manifest() => _cutStorage.Received(1).PublishManifest(Arg.Any<Storage.Cuts.ReadModelCutManifest>());
}
