// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_the_latest_key_can_decrypt : given.a_rotated_key
{
    void Establish() => _latest = _old;

    async Task Because() => await Release();

    [Fact] void should_preserve_the_fast_path() => _released.GetValue<string>().ShouldEqual("value");
    [Fact] void should_not_probe_older_revisions() => _storage.DidNotReceiveWithAnyArgs().TryGetAtRevisionFor(default, default, default, default);
}
