// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using ContractCuts = Cratis.Chronicle.Contracts.Cuts;

namespace Cratis.Chronicle.Services.Cuts.for_ReadModelCuts.when_capturing;

public class and_the_read_model_is_projection_backed : given.all_dependencies
{
    ContractCuts.ReadModelCutResponse _result;

    void Establish() => _projection.Process(Arg.Any<Concepts.EventStoreNamespaceName>(), Arg.Any<IEnumerable<AppendedEvent>>())
        .Returns([new ExpandoObject()]);

    async Task Because() => _result = await _service.Capture(new ContractCuts.ReadModelCutRequest
    {
        EventStore = "my-event-store",
        Namespace = "my-namespace",
        Cuts = [new ContractCuts.EventSequenceCut { EventSequenceId = EventSequence, Position = 5UL }],
        Selection = [ReadModel]
    });

    [Fact] void should_return_exactly_one_entry() => _result.Entries.Count().ShouldEqual(1);
    [Fact] void should_report_the_entry_as_captured() => _result.Entries.Single().Outcome.ShouldEqual(ContractCuts.ReadModelCutOutcome.Captured);
    [Fact] void should_report_the_read_model_identifier() => _result.Entries.Single().ReadModel.ShouldEqual(ReadModel.Value);
    [Fact] void should_include_a_digest() => _result.Entries.Single().Digest.ShouldNotBeNull();
    [Fact] void should_save_the_payload() => _cutStorage.Received(1).SavePayload(Arg.Any<Concepts.Cuts.ReadModelCutId>(), ReadModel, Arg.Any<string>());
    [Fact] void should_publish_the_manifest() => _cutStorage.Received(1).PublishManifest(Arg.Any<Storage.Cuts.ReadModelCutManifest>());
    [Fact] void should_read_events_only_up_to_the_requested_cut() => _eventSequenceStorage.Received(1).GetRange(EventSequenceNumber.First, 5UL);
}
