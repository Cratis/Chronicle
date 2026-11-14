// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.ProtectedValues.for_ProtectedValueRelease;

public class when_v1_authentication_fails : given.a_rotated_key
{
    void Establish()
    {
        _latest = _old;
        _encrypted[^1] ^= 1;
    }

    async Task Because() => _failure = await Catch.Exception(Release);

    [Fact] void should_preserve_the_authentication_failure() => _failure.ShouldBeOfExactType<AuthenticationTagMismatchException>();
    [Fact] void should_not_try_other_revisions() => _storage.DidNotReceiveWithAnyArgs().TryGetAtRevisionFor(default, default, default, default);
}
