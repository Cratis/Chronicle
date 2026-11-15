// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership.given;

public class a_connection_ownership : Specification
{
    protected static readonly ConnectionId Id = new("the-connection");
    protected static readonly ConnectionOwner Alice = new("issuer", "alice");
    protected static readonly ConnectionOwner Bob = new("issuer", "bob");
    protected ManualClock _clock;
    protected ConnectionOwnership _ownership;

    void Establish()
    {
        _clock = new();
        _ownership = new(_clock);
    }

    protected sealed class ManualClock : TimeProvider
    {
        DateTimeOffset _now = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public void Advance(TimeSpan by) => _now += by;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
