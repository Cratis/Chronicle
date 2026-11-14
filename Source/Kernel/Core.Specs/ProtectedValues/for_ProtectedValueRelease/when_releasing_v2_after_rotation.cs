// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_releasing_v2_after_rotation : given.a_rotated_key
{
    void Establish() => _encrypted = Compliance.Encryption.EncryptEnvelope("\"value\""u8.ToArray(), _old, 2);

    async Task Because() => await Release();

    [Fact] void should_use_the_recorded_revision() => _released.GetValue<string>().ShouldEqual("value");
    [Fact] void should_not_read_the_latest_key() => _storage.DidNotReceiveWithAnyArgs().TryGetLatestFor(default, default, default);
    [Fact] void should_read_only_one_revision() => _storage.ReceivedWithAnyArgs(1).TryGetAtRevisionFor(default, default, default, default);
}
