// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Grpc.Core;

namespace Cratis.Chronicle.ReadModelLookups.for_ReadModelLookup;

public class when_the_database_is_marked : when_finding_by_composite_key.given.a_composite_key_read
{
    Exception? _error;

    async Task Establish()
    {
        var marker = new InMemorySinkProvenanceStorage();
        await marker.MarkQuarantined();
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).GetNamespace(Arg.Any<EventStoreNamespaceName>()).Sinks.GetProvenance(Arg.Any<ReadModelDefinition>()).Returns(marker);
        _provenance = new ReadModelSinkProvenance(storage);
        _parts = new Dictionary<string, string> { ["unknown"] = "not-json" };
    }

    async Task Because() => _error = await Catch.Exception(Read);

    [Fact] void should_refuse_before_validating_the_supplied_key() => ((RpcException)_error!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_use_the_neutral_refusal() => ((RpcException)_error!).Status.Detail.ShouldEqual(SystemStoreAccess.Refused);
    [Fact] Task should_not_look_up_the_payload() => DidNotLookUp();
    [Fact] void should_not_serialize_a_payload() => _converter.ReceivedCalls().Any().ShouldBeFalse();
}
