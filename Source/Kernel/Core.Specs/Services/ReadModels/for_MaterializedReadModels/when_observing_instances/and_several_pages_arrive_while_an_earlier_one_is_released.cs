// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_MaterializedReadModels.when_observing_instances;

/// <summary>
/// Under sustained writes the sink emits faster than a slow release can drain. Every page that queued behind the one
/// being released used to cost a release, a count and a page read, and the client was served them one by one long after
/// a newer page existed. Only the latest page is kept waiting now, so the pages in between are never processed.
/// </summary>
public class and_several_pages_arrive_while_an_earlier_one_is_released : for_ReadModels.given.all_dependencies
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);

    readonly TaskCompletionSource<IEnumerable<ExpandoObject>> _slowRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _firstReleaseStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _lastPageSent = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<ObserveInstancesResponse> _responses = [];
    readonly List<string> _released = [];
    MaterializedReadModels _materializedService;
    ReplaySubject<IEnumerable<ExpandoObject>> _pages;

    void Establish()
    {
        _pages = new();
        _pages.OnNext([Named("first")]);
        _pages.OnNext([Named("second")]);
        _pages.OnNext([Named("third")]);
        _pages.OnNext([Named("fourth")]);
        _sink.ObserveInstances(Arg.Any<ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>()).Returns(_pages);
        _sink.GetInstances(Arg.Any<ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>()).Returns(new ReadModelInstances([], 4));

        _complianceHelper.Release(
            Arg.Any<EventStoreName>(),
            Arg.Any<EventStoreNamespaceName>(),
            Arg.Any<JsonSchema>(),
            Arg.Any<IEnumerable<ExpandoObject>>())
            .Returns(callInfo =>
            {
                var instances = callInfo.ArgAt<IEnumerable<ExpandoObject>>(3).ToList();
                bool isFirst;
                lock (_released)
                {
                    _released.Add(NameOf(instances[0]));
                    isFirst = _released.Count == 1;
                }

                if (isFirst)
                {
                    _firstReleaseStarted.TrySetResult();
                    return _slowRelease.Task;
                }

                return Task.FromResult<IEnumerable<ExpandoObject>>(instances);
            });

        _materializedService = new(_grainFactory, _storage, _complianceHelper);
    }

    async Task Because()
    {
        using var subscription = _materializedService.ObserveInstances(new()
        {
            EventStore = "test-store",
            Namespace = "test-namespace",
            ReadModel = "test-read-model",
            PageSize = 10
        }).Subscribe(
            response =>
            {
                lock (_responses)
                {
                    _responses.Add(response);
                    if (_responses.Count == 2)
                    {
                        _lastPageSent.TrySetResult();
                    }
                }
            },
            error => _lastPageSent.TrySetException(error));

        await _firstReleaseStarted.Task.WaitAsync(_deadline);
        _slowRelease.SetResult([Named("first")]);
        await _lastPageSent.Task.WaitAsync(_deadline);

        // Let anything that would still be queued after the latest page show itself before asserting.
        await Task.Delay(200);
    }

    [Fact] void should_send_two_pages() => _responses.Count.ShouldEqual(2);
    [Fact] void should_send_the_page_being_released_first() => _responses[0].Instances.Single().ShouldContain("first");
    [Fact] void should_send_the_latest_page_last() => _responses[1].Instances.Single().ShouldContain("fourth");
    [Fact] void should_release_only_the_first_and_the_latest_page() => _released.ShouldContainOnly(["first", "fourth"]);

    static ExpandoObject Named(string name)
    {
        var instance = new ExpandoObject();
        ((IDictionary<string, object?>)instance)["name"] = name;
        return instance;
    }

    static string NameOf(ExpandoObject instance) => (string)((IDictionary<string, object?>)instance)["name"]!;
}
