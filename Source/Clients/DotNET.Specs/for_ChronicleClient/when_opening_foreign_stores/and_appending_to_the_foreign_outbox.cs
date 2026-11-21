// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores;

public class and_appending_to_the_foreign_outbox : given.a_client_with_an_own_store
{
    AppendRequest _append;
    AppendResult _result;

    void Establish() => _services.Sequences.Append(Arg.Any<AppendRequest>()).Returns(call =>
    {
        _append = call.Arg<AppendRequest>();
        return new CommandResult<AppendResponse> { Response = new() { IsSuccess = true, SequenceNumber = 42 } };
    });

    async Task Because()
    {
        _foreign = await _client.GetEventStore("foreign", "tenant", RegistrationMode.AtDeployment);
        _result = await _foreign.GetEventSequence(EventSequenceId.Outbox).Append("source", new PlainEvent("published"));
    }

    [Fact] void should_append_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_target_foreign_store() => _append.EventStore.ShouldEqual("foreign");
    [Fact] void should_target_foreign_namespace() => _append.Namespace.ShouldEqual("tenant");
    [Fact] void should_target_outbox() => _append.EventSequenceId.ShouldEqual(EventSequenceId.Outbox.Value);
    [Fact] void should_use_discovered_identity() => _append.EventType.Id.ShouldEqual("plain");
    [Fact] void should_serialize_the_event() => _append.Content.ShouldContain("published");
    [Fact] void should_not_register_foreign_artifacts() => ShouldNotRegisterForeignArtifacts();
}
