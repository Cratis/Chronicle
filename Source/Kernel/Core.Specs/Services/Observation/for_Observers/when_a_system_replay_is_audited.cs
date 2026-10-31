// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Services.Observation.for_Observers;

public class when_a_system_replay_is_audited : given.an_operation_authorization_policy
{
    IObserver _observer;
    ReplayResponse _response;

    void Establish()
    {
        ConfigurePolicy(enforce: false);
        _observer = Substitute.For<IObserver>();
        _observer.Replay().Returns(JobId.NotSet);
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
    }

    async Task Because() => _response = await _observers.Replay(
        new Replay
        {
            EventStore = "System", Namespace = "default", ObserverId = "observer", EventSequenceId = "event-log"
        },
        _callContext);

    [Fact] void should_keep_the_existing_rpc_result() => _response.JobId.ShouldEqual(JobId.NotSet.Value.ToString());
    [Fact] void should_dispatch_to_the_observer() => _observer.Received(1).Replay();
    [Fact] void should_log_the_caller_kind_and_contract_operation() => _logger.Entries.Single().Message.ShouldContain("app calling Replay");
    [Fact] void should_not_log_the_token() => _logger.Entries.Single().Message.ShouldNotContain("do-not-log-this-token");
    [Fact] void should_use_the_grpc_callers_principal() => _administratorAuthorization.Received(1).IsAuthorized(_principal);
}
