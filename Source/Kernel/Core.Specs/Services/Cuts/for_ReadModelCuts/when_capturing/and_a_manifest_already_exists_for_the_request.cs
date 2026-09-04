// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Cuts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Cuts;
using ContractCuts = Cratis.Chronicle.Contracts.Cuts;

namespace Cratis.Chronicle.Services.Cuts.for_ReadModelCuts.when_capturing;

public class and_a_manifest_already_exists_for_the_request : given.all_dependencies
{
    ReadModelCutManifest _existingManifest;
    ContractCuts.ReadModelCutResponse _result;

    void Establish()
    {
        _existingManifest = new ReadModelCutManifest(
            ReadModelCutId.NotSet,
            "my-event-store",
            "my-namespace",
            [new Concepts.Cuts.EventSequenceCut(EventSequence, 5UL)],
            [new ReadModelCutEntry(ReadModel, Concepts.Cuts.ReadModelCutOutcome.Captured, (ReadModelGeneration)1, null, null)],
            DateTimeOffset.UnixEpoch);
        _cutStorage.GetManifest(Arg.Any<ReadModelCutId>()).Returns(_existingManifest);
    }

    async Task Because() => _result = await _service.Capture(new ContractCuts.ReadModelCutRequest
    {
        EventStore = "my-event-store",
        Namespace = "my-namespace",
        Cuts = [new ContractCuts.EventSequenceCut { EventSequenceId = EventSequence, Position = 5UL }],
        Selection = [ReadModel]
    });

    [Fact] void should_return_the_existing_manifests_entries() => _result.Entries.Single().ReadModel.ShouldEqual(ReadModel.Value);
    [Fact] void should_not_look_up_projection_definitions() => _projectionsManager.DidNotReceive().GetProjectionDefinitions();
    [Fact] void should_not_publish_a_new_manifest() => _cutStorage.DidNotReceive().PublishManifest(Arg.Any<ReadModelCutManifest>());
}
