// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.EventSequences.Concurrency;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_a_single_source_batch_has_no_events : given.an_event_sequence_with_metadata
{
    AppendManyResult _result;
    Contracts.Sequences.AppendManyRequest _request;

    void Establish() => _sequences.AppendMany(Arg.Any<Contracts.Sequences.AppendManyRequest>(), Arg.Any<CallContext>())
        .Returns(call =>
        {
            _request = call.Arg<Contracts.Sequences.AppendManyRequest>();
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(
                _correlationId, new() { CorrelationId = _correlationId, SequenceNumbers = [], ConstraintViolations = [], ConcurrencyViolations = [], Errors = [] });
        });

    async Task Because() => _result = await _eventSequence.AppendMany(_source, [], concurrencyScope: new ConcurrencyScope(0, _source));

    [Fact] void should_validate_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_notify_subscribers() => _notifications.ShouldBeNull();
    [Fact] void should_send_the_empty_batch() => _request.Events.ShouldBeEmpty();
}
