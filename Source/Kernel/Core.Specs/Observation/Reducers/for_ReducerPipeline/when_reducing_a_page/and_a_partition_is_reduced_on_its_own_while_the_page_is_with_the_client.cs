// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_reducing_a_page;

/// <summary>
/// A retry or recovery reduces one partition through the same shared sink while a page is waiting for the client. Its
/// write must be durable when it reports success; a bulk window opened by the page would buffer it instead.
/// </summary>
public class and_a_partition_is_reduced_on_its_own_while_the_page_is_with_the_client : given.a_page_pipeline
{
    readonly TaskCompletionSource _pageIsWithTheClient = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _clientAnswers = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _durableWhenReported;
    bool _windowOpenWhileReducing;

    async Task Because()
    {
        var page = _pipeline.ReducePage(Page(), async partitions =>
        {
            _pageIsWithTheClient.SetResult();
            await _clientAnswers.Task;
            return await Reduce(partitions);
        });
        await _pageIsWithTheClient.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _windowOpenWhileReducing = _sink.IsInBulkMode;

        await _pipeline.Reduce(
            new ReducerContext([CreateEvent("recovered", 10)], new Key("recovered", Properties.ArrayIndexers.NoIndexers)),
            (_, _) => Task.FromResult(new ReducerSubscriberResult(ObserverSubscriberResult.Ok(EventSequenceNumber.First), StateWith(7))));
        _durableWhenReported = _sink.Durable.Contains("recovered");

        _clientAnswers.SetResult();
        _outcome = await page.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_not_have_a_window_open_while_the_page_is_with_the_client() => _windowOpenWhileReducing.ShouldBeFalse();
    [Fact] void should_have_made_the_single_partition_write_durable_before_reporting_it() => _durableWhenReported.ShouldBeTrue();
    [Fact] void should_still_write_every_partition_of_the_page() => Partitions.All(_sink.Durable.Contains).ShouldBeTrue();
    [Fact] void should_never_begin_a_window() => _sink.Calls.ShouldNotContain("BeginBulk");
}
