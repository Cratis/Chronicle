// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_an_older_kernel_reports_node_loss : given.an_event_sequence
{
    readonly CommandResult<Contracts.Sequences.AppendResponse> _failure = new()
    {
        ExceptionMessages = ["target silo became unavailable"]
    };
    Exception _exception;

    void Establish()
    {
        _identityProvider.GetCurrent().Returns(Identities.Identity.System);
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("event", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<string>()).Returns(new JsonObject());
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), CallContext.Default).Returns(_failure);
    }

    async Task Because() => _exception = await Catch.Exception(() => _eventSequence.Append("source", "event"));

    [Fact] void should_preserve_the_command_failure() => _exception.ShouldBeOfExactType<CommandFailed>();
    [Fact] void should_preserve_the_original_error() => ((CommandFailed)_exception).Result.ExceptionMessages.ShouldContainOnly(_failure.ExceptionMessages);
}
