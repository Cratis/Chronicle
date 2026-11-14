// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_the_key_read_fails_operationally : given.a_rotated_key
{
    readonly InvalidOperationException _storageFailure = new("storage unavailable");

    void Establish() => _storage.TryGetLatestFor(Address.Item1, Address.Item2, Address.Item3).Returns<Storage.Compliance.EncryptionKeyAtRevision?>(_ => throw _storageFailure);

    async Task Because() => _failure = await Catch.Exception(Release);

    [Fact] void should_keep_the_operational_failure() => _failure.ShouldEqual(_storageFailure);
    [Fact] void should_not_probe_older_keys() => _storage.DidNotReceiveWithAnyArgs().TryGetAtRevisionFor(default, default, default, default);
}
