// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_observing;

public class without_a_filter : given.a_collection_stream
{
    async Task Because()
    {
        _observation = _readModels.Observe<Row>();
        Subscribe();
        await Emit(Enumerable.Range(0, 75).Select(index => new Row($"work-{index}", $"Item {index}")).ToArray());
    }

    [Fact] void should_request_the_whole_collection() => _request.PageSize.ShouldEqual(int.MaxValue);
    [Fact] void should_start_at_the_beginning() => _request.Page.ShouldEqual(0);
    [Fact] void should_emit_more_than_fifty_instances() => _emissions.Single().Count().ShouldEqual(75);
    [Fact] void should_preserve_the_domain_named_key() => _emissions.Single().First().WorkItemId.ShouldEqual("work-0");
    [Fact] void should_use_the_current_namespace() => _request.Namespace.ShouldEqual(_eventStore.Namespace.Value);
    [Fact] void should_not_use_the_shared_watcher() => _readModelWatcherManager.ReceivedCalls().ShouldBeEmpty();
}
