// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_real_ownership;

/// <summary>
/// The cluster-wide lookup is made of grain calls, so a lookup that outlasts the grace must not keep the client waiting.
/// </summary>
public class a_claim_whose_cluster_lookup_outlasts_the_grace : given.a_service_with_real_ownership
{
    Exception? _outcome;
    bool _owned;
    int _registrations;

    void Establish() => _hostsLookup = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Because()
    {
        _outcome = await Open(AuthenticatedCalls.As("alice"));
        _owned = Owns("alice");
        _registrations = Calls(nameof(IConnectedClients.OnClientConnected));
    }

    void Destroy() => _hostsLookup.TrySetResult();

    [Fact] void should_serve_the_client_without_waiting_for_the_lookup() => _outcome.ShouldBeNull();
    [Fact] void should_have_claimed_the_connection() => _owned.ShouldBeTrue();
    [Fact] void should_have_registered_the_client() => _registrations.ShouldEqual(1);
}
