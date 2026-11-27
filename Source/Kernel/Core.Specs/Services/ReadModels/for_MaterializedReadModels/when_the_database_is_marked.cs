// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Security;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_MaterializedReadModels;

public class when_the_database_is_marked : for_ReadModels.given.a_database_with_provenance
{
    Exception? _getError;
    Exception? _observeError;
    bool _wasCold;

    async Task Because()
    {
        var service = new MaterializedReadModels(_grainFactory, _readBindings, _complianceHelper, NullLogger<MaterializedReadModels>.Instance, NamespaceCasingContext.Create());
        var stream = service.ObserveInstances(new() { EventStore = "store", Namespace = "namespace", ReadModel = "test-read-model", Occurrence = "retained-copy" });
        _wasCold = !_sinks.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "GetProvenance");
        await _marker.MarkQuarantined();
        _getError = await Catch.Exception(() => service.GetInstances(new() { EventStore = "store", Namespace = "namespace", ReadModel = "test-read-model", Occurrence = "retained-copy" }));
        _observeError = await Catch.Exception(() => stream.FirstAsync().ToTask().WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact] void should_check_the_stream_at_subscription() => _wasCold.ShouldBeTrue();
    [Fact] void should_refuse_the_unary_read() => ((RpcException)_getError!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_refuse_the_stream_through_its_error_channel() => ((RpcException)_observeError!).Status.Detail.ShouldEqual(SystemStoreAccess.Refused);
    [Fact] void should_not_resolve_a_payload_sink_or_occurrence() => _sinks.DidNotReceiveWithAnyArgs().GetFor(default!);
    [Fact] void should_not_read_or_subscribe_to_payloads() => _sink.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_release_payloads() => _complianceHelper.ReceivedCalls().Any().ShouldBeFalse();
}
