// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.ReadModels.for_MaterializedReadModels.when_observing_instances;

public class and_an_earlier_page_is_released_slower_than_a_later_one : for_ReadModels.given.all_dependencies
{
    static readonly TimeSpan _deadline = TimeSpan.FromSeconds(10);

    readonly TaskCompletionSource<IEnumerable<ExpandoObject>> _slowRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _firstReleaseStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _bothPagesSent = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<ObserveInstancesResponse> _responses = [];
    MaterializedReadModels _materializedService;
    ReplaySubject<IEnumerable<ExpandoObject>> _pages;
    int _releases;

    void Establish()
    {
        _pages = new();
        _pages.OnNext([Named("first")]);
        _pages.OnNext([Named("second")]);
        _sink.ObserveInstances(Arg.Any<ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>()).Returns(_pages);
        _sink.GetInstances(Arg.Any<ReadModelContainerName?>(), Arg.Any<int>(), Arg.Any<int>()).Returns(new ReadModelInstances([], 2));

        _complianceHelper.Release(
            Arg.Any<EventStoreName>(),
            Arg.Any<EventStoreNamespaceName>(),
            Arg.Any<JsonSchema>(),
            Arg.Any<IEnumerable<ExpandoObject>>())
            .Returns(callInfo =>
            {
                var instances = callInfo.ArgAt<IEnumerable<ExpandoObject>>(3).ToList();
                if (Interlocked.Increment(ref _releases) == 1)
                {
                    _firstReleaseStarted.TrySetResult();
                    return _slowRelease.Task;
                }

                return Task.FromResult<IEnumerable<ExpandoObject>>(instances);
            });

        _materializedService = new(_grainFactory, _storage, _complianceHelper, NullLogger<MaterializedReadModels>.Instance);
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
                        _bothPagesSent.TrySetResult();
                    }
                }
            },
            error => _bothPagesSent.TrySetException(error));

        await _firstReleaseStarted.Task.WaitAsync(_deadline);
        _slowRelease.SetResult([Named("first")]);
        await _bothPagesSent.Task.WaitAsync(_deadline);
    }

    [Fact] void should_send_the_earlier_page_first() => _responses[0].Instances.Single().ShouldContain("first");
    [Fact] void should_send_the_later_page_last() => _responses[1].Instances.Single().ShouldContain("second");

    static ExpandoObject Named(string name)
    {
        var instance = new ExpandoObject();
        ((IDictionary<string, object?>)instance)["name"] = name;
        return instance;
    }
}
