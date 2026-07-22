// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_activity_listener_is_attached : given.a_unit_of_work
{
    readonly ConcurrentBag<Activity> _startedActivities = [];
    ActivityListener _listener;
    Activity _commitActivity;

    void Establish()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ClientActivity.SourceName,
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStarted = _startedActivities.Add
        };
        ActivitySource.AddActivityListener(_listener);
    }

    async Task Because()
    {
        await _unitOfWork.Commit();
        _commitActivity = _startedActivities.FirstOrDefault(activity => Equals(activity.GetTagItem("correlation_id"), _correlationId.ToString()));
    }

    [Fact] void should_start_activity_tagged_with_correlation_id() => _commitActivity.ShouldNotBeNull();
    [Fact] void should_use_commit_span_name() => _commitActivity.OperationName.ShouldEqual("client.unit_of_work.commit");

    void Destroy() => _listener.Dispose();
}
