// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_real_ownership;

public class a_claim_whose_cluster_lookup_is_delayed : given.a_service_with_real_ownership
{
    bool _answeredWhileDelayed;
    bool _ownedWhileDelayed;
    int _registrationsWhileDelayed;
    Exception? _outcome;
    bool _ownedAfterwards;

    void Establish() => _hostsLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Because()
    {
        var opening = Open(AuthenticatedCalls.As("alice"));
        await _hostsLookupRequested.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        _answeredWhileDelayed = opening.IsCompleted;
        _ownedWhileDelayed = Owns("alice");
        _registrationsWhileDelayed = Calls(nameof(IConnectedClients.OnClientConnected));

        _hostsLookup.SetResult();
        _outcome = await opening;
        _ownedAfterwards = Owns("alice");
    }

    [Fact] void should_not_have_answered_the_client_while_the_lookup_was_pending() => _answeredWhileDelayed.ShouldBeFalse();
    [Fact] void should_not_have_claimed_the_connection_while_the_lookup_was_pending() => _ownedWhileDelayed.ShouldBeFalse();
    [Fact] void should_not_have_registered_the_client_while_the_lookup_was_pending() => _registrationsWhileDelayed.ShouldEqual(0);
    [Fact] void should_answer_once_the_lookup_completes() => _outcome.ShouldBeNull();
    [Fact] void should_have_claimed_the_connection_by_then() => _ownedAfterwards.ShouldBeTrue();
}
