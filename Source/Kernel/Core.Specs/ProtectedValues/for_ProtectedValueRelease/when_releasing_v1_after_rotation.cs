// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_releasing_v1_after_rotation : given.a_rotated_key
{
    async Task Because() => await Release();

    [Fact] void should_recover_with_the_retained_revision() => _released.GetValue<string>().ShouldEqual("value");
    [Fact] void should_skip_the_missing_revision() => _storage.Received(1).TryGetAtRevisionFor(Address.Item1, Address.Item2, Address.Item3, (EncryptionKeyRevision)3u);
    [Fact] void should_probe_only_the_same_identity() => _storage.ReceivedWithAnyArgs(2).TryGetAtRevisionFor(default, default, default, default);
    [Fact] void should_not_provision_any_keys() => _storage.DidNotReceiveWithAnyArgs().GetOrAddFor(default, default, default, default);
}
