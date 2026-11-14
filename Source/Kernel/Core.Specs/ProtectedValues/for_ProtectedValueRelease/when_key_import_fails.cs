// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_key_import_fails : given.a_rotated_key
{
    void Establish() => _latest = new(new EncryptionKey([], []), 4);

    async Task Because() => _failure = await Catch.Exception(Release);

    [Fact] void should_preserve_the_import_failure() => _failure.ShouldBeOfExactType<CryptographicException>();
    [Fact] void should_not_try_other_revisions() => _storage.DidNotReceiveWithAnyArgs().TryGetAtRevisionFor(default, default, default, default);
}
