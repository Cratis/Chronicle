// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Server.for_KernelHost;

/// <summary>
/// Enforcing store entitlements reads the configured grants through a grain to warn about an empty set. Done from a
/// hosted service, that call ran before the listeners bound and before the deferred silo started, so neither ever
/// happened (#4040). It now runs once the silo is active, and the listeners bind first.
/// </summary>
public class when_store_entitlements_are_enforced_and_the_silo_is_held : given.a_held_silo
{
    HttpStatusCode _live;
    HttpStatusCode _ready;
    HttpStatusCode _afterRelease;

    protected override bool AuthenticationEnabled => true;
    protected override bool EnforceStoreEntitlements => true;

    async Task Because()
    {
        await StartHeld();
        using var live = await _client.GetAsync("/health/live");
        using var ready = await _client.GetAsync("/health/ready");
        _live = live.StatusCode;
        _ready = ready.StatusCode;
        _hold.Release.TrySetResult();
        await _start.WaitAsync(TimeSpan.FromSeconds(10));
        using var afterRelease = await _client.GetAsync("/health/ready");
        _afterRelease = afterRelease.StatusCode;
    }

    [Fact] void should_bind_liveness_before_the_silo_is_ready() => _live.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_hold_readiness() => _ready.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_complete_startup() => _afterRelease.ShouldEqual(HttpStatusCode.OK);
}
