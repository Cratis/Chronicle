// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Contracts.Compliance;
using Cratis.Chronicle.Contracts.ReadModels;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.ReadModels.for_MaterializedReadModels.when_observing_instances;

/// <summary>
/// Pages are released one after the other, so an earlier page whose release is slow is still delivered before a
/// later one - otherwise the subscriber would end up holding the earlier, stale page.
/// </summary>
public class and_an_earlier_page_is_released_slower_than_a_later_one : given.a_recording_compliance_service
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);

    readonly TaskCompletionSource<ReleaseResponse> _slowRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _firstReleaseStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _bothPagesDelivered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<Employee[]> _pages = [];

    record Employee(string Id, [property: PII] string Name);

    void Establish()
    {
        // The surface asks with the type, so that is the overload to answer.
#pragma warning disable CA2263
        _projections.HasFor(typeof(Employee)).Returns(true);
#pragma warning restore CA2263
        var materialized = Substitute.For<Contracts.ReadModels.IMaterializedReadModels>();
        _services.MaterializedReadModels.Returns(materialized);

        var responses = new ReplaySubject<ObserveInstancesResponse>();
        responses.OnNext(new() { Instances = [Json("first")] });
        responses.OnNext(new() { Instances = [Json("second")] });
        materialized.ObserveInstances(Arg.Any<ObserveInstancesRequest>(), Arg.Any<CallContext>()).Returns(responses);

        _compliance.Release(Arg.Any<ReleaseRequest>()).Returns(call =>
        {
            var request = call.Arg<ReleaseRequest>();
            if (request.Subject == "first")
            {
                _firstReleaseStarted.TrySetResult();
                return _slowRelease.Task;
            }

            return Task.FromResult(new ReleaseResponse { Payload = request.Payload });
        });
    }

    async Task Because()
    {
        using var subscription = _readModels.Materialized.ObserveInstances<Employee>().Subscribe(
            page =>
            {
                lock (_pages)
                {
                    _pages.Add([.. page]);
                    if (_pages.Count == 2)
                    {
                        _bothPagesDelivered.TrySetResult();
                    }
                }
            },
            error => _bothPagesDelivered.TrySetException(error));

        await _firstReleaseStarted.Task.WaitAsync(_deadline);
        _slowRelease.SetResult(new ReleaseResponse { Payload = Json("first") });
        await _bothPagesDelivered.Task.WaitAsync(_deadline);
    }

    [Fact] void should_deliver_the_earlier_page_first() => _pages[0].Single().Id.ShouldEqual("first");
    [Fact] void should_deliver_the_later_page_last() => _pages[1].Single().Id.ShouldEqual("second");

    static string Json(string id) => $$"""{"Id":"{{id}}","Name":"{{id}}"}""";
}
