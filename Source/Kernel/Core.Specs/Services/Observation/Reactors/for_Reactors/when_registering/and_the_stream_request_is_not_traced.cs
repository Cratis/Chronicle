// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors.when_registering;

public class and_the_stream_request_is_not_traced : given.an_observation_stream
{
    readonly ActivitySource _source = new($"Chronicle.Reactors.Specs.{Guid.NewGuid():N}");
    readonly TaskCompletionSource<Activity> _registered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ActivityListener _listener;
    Activity _request;
    Activity _registration;

    private protected override IActivitySource<Reactors> CreateActivitySource()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _source.Name,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _registered.TrySetResult(activity)
        };
        ActivitySource.AddActivityListener(_listener);
        _request = new Activity("Observe request").Start();
        return new ActivitySource<Reactors>(_source);
    }

    async Task Because() => _registration = await _registered.Task.WaitAsync(TimeSpan.FromSeconds(5));

    [Fact] void should_keep_the_register_operation() => _registration.OperationName.ShouldEqual("cratis.chronicle.reactors.register");
    [Fact] void should_not_parent_registration_to_the_request() => _registration.ParentId.ShouldBeNull();

    void Destroy()
    {
        _request.Dispose();
        _listener.Dispose();
        _source.Dispose();
    }
}
