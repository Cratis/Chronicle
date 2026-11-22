// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_opening_foreign_stores;

public class and_inline_mode_is_opened_first : given.a_client_with_an_own_store
{
    async Task Because()
    {
        _foreign = await _client.GetEventStore("own", null, RegistrationMode.EventTypesOnly);
        await _foreign.RegisterAll();
    }

    [Fact] void should_separate_modes() => _foreign.ShouldNotBeSame(_own);
    [Fact] void should_not_escalate_to_inline() => ShouldNotRegisterForeignArtifacts();
    [Fact] void should_share_transport() => _foreign.Connection.ShouldBeSame(_own.Connection);
}
