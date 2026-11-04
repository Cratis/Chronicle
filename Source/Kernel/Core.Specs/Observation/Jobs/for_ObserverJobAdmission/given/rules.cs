// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverJobAdmission.given;

public class rules : Specification
{
    protected IStorage _storage;
    protected List<int> _asked;
    protected IObserverJobRequest _request;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _asked = [];
        _request = new CatchUpObserverRequest(new("observer", "event-store", "namespace", "event-sequence"), ObserverType.Reactor, EventSequenceNumber.First, []);
    }

    protected IObserverJobAdmissionRule Rule(int order, bool admits)
    {
        var rule = Substitute.For<IObserverJobAdmissionRule>();
        rule.Order.Returns(order);
        rule.MayRun(Arg.Any<ObserverJobAdmissionContext>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _asked.Add(order);
            return Task.FromResult(admits);
        });
        return rule;
    }

    protected static JobAdmissionRequest AdmissionFor(IJobRequest request) => new(JobKey.NotSet, JobId.New(), JobType.NotSet, request);
}
