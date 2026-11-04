// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients.for_ConnectionOwnership;

public class when_replacing_a_subscription_in_the_same_generation : Specification
{
    readonly ConnectionOwnership _ownership = new();
    object _old;
    object _current;

    async Task Because()
    {
        using var gate = await _ownership.Enter("connection");
        _ownership.Claim("connection", ConnectionOwner.InProcess);
        _old = _ownership.Subscribe("connection", "observer");
        _current = _ownership.Subscribe("connection", "observer");
        _ownership.ReleaseSubscription("connection", "observer", _old);
    }

    [Fact] void should_retire_the_old_stream() => _ownership.IsCurrentSubscription("connection", "observer", _old).ShouldBeFalse();
    [Fact] void should_not_release_the_new_stream() => _ownership.IsCurrentSubscription("connection", "observer", _current).ShouldBeTrue();
}
