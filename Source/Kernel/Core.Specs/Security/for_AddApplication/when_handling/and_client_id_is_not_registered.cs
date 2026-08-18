// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AddApplication.when_handling;

public class and_client_id_is_not_registered : given.an_add_application_command
{
    void Establish() => _applications.GetByClientId(Arg.Any<ClientId>(), Arg.Any<CancellationToken>()).Returns((Storage.Security.Application?)null);

    async Task Because() => await _command.Handle(_grainFactory, _storage);

    [Fact] void should_append_application_added_for_the_application() => _eventLog.Received(1).Append(
        (EventSourceId)ApplicationIdentifier.ToString(),
        Arg.Is<ApplicationAdded>(_ => _.ClientId == (ClientId)ClientIdentifier),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Causation>>(),
        Arg.Any<Identity>(),
        Arg.Any<IEnumerable<Tag>>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>());

    [Fact] void should_store_a_hashed_secret_rather_than_the_plain_text_one() => _eventLog.Received(1).Append(
        Arg.Any<EventSourceId>(),
        Arg.Is<ApplicationAdded>(_ => _.ClientSecret.Value != Secret && !string.IsNullOrEmpty(_.ClientSecret.Value)),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Causation>>(),
        Arg.Any<Identity>(),
        Arg.Any<IEnumerable<Tag>>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>());
}
