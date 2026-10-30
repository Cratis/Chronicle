// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.for_KernelHost;

public class when_authentication_is_enabled_and_the_silo_is_held : given.a_held_silo
{
    HttpStatusCode _live;
    HttpStatusCode _ready;
    HttpStatusCode _afterRelease;
    bool _valid;
    bool _invalid;

    protected override bool AuthenticationEnabled => true;

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
        var tokens = _app.Services.GetRequiredService<IInitialAdminSetupTokens>();
        var user = new UserId(Guid.NewGuid());
        var token = tokens.Issue(user);
        _valid = tokens.IsValid(user, token);
        _invalid = tokens.IsValid(new UserId(Guid.NewGuid()), token);
    }

    [Fact] void should_bind_liveness_before_the_silo_is_ready() => _live.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_hold_readiness() => _ready.ShouldEqual(HttpStatusCode.ServiceUnavailable);
    [Fact] void should_complete_startup() => _afterRelease.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_issue_a_valid_setup_token_through_the_shared_grain() => _valid.ShouldBeTrue();
    [Fact] void should_reject_a_token_for_another_user() => _invalid.ShouldBeFalse();
}
