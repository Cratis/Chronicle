// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AddApplication.when_handling;

/// <summary>
/// A client id identifies an OAuth client to OpenIddict, so registering it twice would leave token issuance
/// depending on which of the two records the store happens to return - the command must refuse instead.
/// </summary>
public class and_client_id_is_already_registered : given.an_add_application_command
{
    Exception _exception;

    void Establish() => _applications.GetByClientId(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns(new Storage.Security.Application
    {
        Id = Guid.NewGuid(),
        ClientId = ClientIdentifier
    });

    async Task Because() => _exception = await Catch.Exception(async () => await _command.Handle(_grainFactory, _storage));

    [Fact] void should_reject_the_command() => _exception.ShouldBeOfExactType<ApplicationClientIdAlreadyRegistered>();
    [Fact] void should_name_the_client_id_in_the_message() => _exception.Message.ShouldContain(ClientIdentifier);
    [Fact] void should_not_append_any_event() => _eventLog.DidNotReceive().Append(
        Arg.Any<EventSourceId>(),
        Arg.Any<object>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Causation>>(),
        Arg.Any<Identity>(),
        Arg.Any<IEnumerable<Tag>>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>());
}
