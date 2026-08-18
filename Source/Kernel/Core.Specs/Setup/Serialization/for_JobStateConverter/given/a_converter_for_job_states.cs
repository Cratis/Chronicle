// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Json;
using Cratis.Monads;

namespace Cratis.Chronicle.Setup.Serialization.for_JobStateConverter.given;

public class a_converter_for_job_states : Specification
{
    protected JsonSerializerOptions _options;
    protected JobType _jobType;
    protected ObserverKey _observerKey;
    protected CatchUpObserverRequest _request;

    void Establish()
    {
        _jobType = new JobType("CatchUpObserver");
        _observerKey = new ObserverKey(
            (ObserverId)"some-observer",
            new EventStoreName("some-event-store"),
            new EventStoreNamespaceName("some-namespace"),
            EventSequenceId.Log);
        _request = new CatchUpObserverRequest(_observerKey, ObserverType.Reactor, EventSequenceNumber.First, []);

        var jobTypes = Substitute.For<IJobTypes>();
        jobTypes.GetRequestClrTypeFor(Arg.Any<JobType>())
            .Returns(Result.Success<Type, IJobTypes.GetRequestClrTypeForError>(typeof(CatchUpObserverRequest)));

        _options = new JsonSerializerOptions(Globals.JsonSerializerOptions);
        _options.Converters.Add(new JobStateConverter(jobTypes));
    }
}
