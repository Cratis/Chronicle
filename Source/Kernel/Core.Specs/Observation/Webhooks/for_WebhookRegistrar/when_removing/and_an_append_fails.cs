// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookRegistrar.when_removing;

public class and_an_append_fails : given.a_webhook_registrar
{
    IEventSequence _eventSequence;
    Exception? _exception;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _grainFactory.GetGrain<IEventSequence>(Arg.Any<string>()).Returns(_eventSequence);
        _eventSequence.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<Identity>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>()).Returns(_ => AppendResult.Failed(CorrelationId.New(), [new AppendError("Missing event schema")]));
    }

    async Task Because() => _exception = await Catch.Exception(async () => await _registrar.Remove("non-system-store", ["webhook"]));

    [Fact] void should_fail_the_removal() => _exception.ShouldBeOfExactType<WebhookRegistrationFailed>();
}
