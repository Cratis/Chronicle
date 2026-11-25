// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Cratis.Chronicle.Security;
using Grpc.Core;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels;

public class when_the_database_is_marked : given.a_database_with_provenance
{
    readonly List<Exception?> _errors = [];
    bool _wasCold;

    async Task Because()
    {
        var watch = _service.Watch(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" });
        _wasCold = !_sinks.ReceivedCalls().Any(call => call.GetMethodInfo().Name == "GetProvenance");
        await _marker.MarkQuarantined();
        _errors.Add(await Catch.Exception(() => _service.GetInstances(new() { EventStore = "store", Namespace = "namespace", ReadModel = "test-read-model" })));
        _errors.Add(await Catch.Exception(() => _service.GetInstanceByKey(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" })));
        _errors.Add(await Catch.Exception(() => _service.GetAllInstances(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" })));
        _errors.Add(await Catch.Exception(() => _service.GetAllInstances(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log", EventCount = 1 })));
        _errors.Add(await Catch.Exception(() => watch.FirstAsync().ToTask().WaitAsync(TimeSpan.FromSeconds(5))));
        _errors.Add(await Catch.Exception(() => _service.DehydrateSession(new() { EventStore = "store", Namespace = "namespace", ReadModelIdentifier = "test-read-model", EventSequenceId = "log" })));
    }

    [Fact] void should_check_watch_when_subscribed_not_when_created() => _wasCold.ShouldBeTrue();
    [Fact] void should_refuse_every_read_even_for_a_kernel_label_without_a_producer() => _errors.TrueForAll(error => error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_use_only_the_neutral_refusal() => _errors.Cast<RpcException>().All(error => error.Status.Detail == SystemStoreAccess.Refused).ShouldBeTrue();
    [Fact] void should_not_acquire_a_payload_sink() => _sinks.DidNotReceiveWithAnyArgs().GetFor(default!);
    [Fact] void should_not_read_payloads() => _sink.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_release_payloads() => _complianceHelper.ReceivedCalls().Any().ShouldBeFalse();
}
