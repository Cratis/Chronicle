// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.for_KernelHost;

public class when_controllers_precede_the_silo_with_concurrent_startup : given.a_held_silo
{
    HttpStatusCode _live;
    bool _valid;
    protected override bool ControllersBeforeSilo => true;
    protected override bool ConcurrentStartup => true;

    async Task Because()
    {
        await StartHeld();
        using var live = await _client.GetAsync("/health/live");
        _live = live.StatusCode;
        _hold.Release.TrySetResult();
        await _start.WaitAsync(TimeSpan.FromSeconds(10));
        var tokens = _app.Services.GetRequiredService<IInitialAdminSetupTokens>();
        var user = new UserId(Guid.NewGuid());
        _valid = tokens.IsValid(user, tokens.Issue(user));
    }

    [Fact] void should_bind_listeners_before_the_silo_can_route() => _live.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_complete_startup_with_a_functioning_shared_provider() => _valid.ShouldBeTrue();
}
