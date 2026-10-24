// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_existing_children_are_keyed_by_another_type : given.a_first_level_child_future
{
    Guid _rootId;

    void Establish()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _rootId = Guid.NewGuid();
        SetFutureKey(Guid.NewGuid());
    }

    async Task Because()
    {
        var root = RootWith();
        dynamic existingChild = new ExpandoObject();
        existingChild.childId = 1;
        ((IDictionary<string, object?>)root)["children"] = new List<ExpandoObject> { existingChild };
        await ProcessRoot(root, _rootId);
    }

    [Fact] void should_leave_the_future_pending() => _resolved.ShouldBeFalse();
    [Fact] void should_not_add_the_child() => _result!.PendingFutureSaves.ShouldBeEmpty();
    [Fact] void should_not_fail_resolving_the_future() => _logger.ReceivedCalls().Count(call =>
        call.GetMethodInfo().Name == nameof(ILogger.Log) && call.GetArguments()[3] is Exception).ShouldEqual(0);
}
