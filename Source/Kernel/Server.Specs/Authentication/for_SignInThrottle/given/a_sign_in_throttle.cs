// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Server.Authentication.for_SignInThrottle.given;

public class a_sign_in_throttle : Specification
{
    protected SignInThrottle _throttle;
    protected Clock _clock;
    protected SignInThrottling _settings = new();

    void Establish() => _clock = new();

    protected void Create() => _throttle = new(
        Options.Create(new ChronicleOptions { Authentication = new() { SignInThrottling = _settings } }),
        _clock);

    protected void Fail(int count, string username = "admin", string address = "192.0.2.1")
    {
        for (var index = 0; index < count; index++) _throttle.Failed(username, address);
    }

    protected class Clock : TimeProvider
    {
        DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(int seconds) => _now = _now.AddSeconds(seconds);
    }
}
