// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces.for_NamespaceCommitRetries;

public class when_no_deadline_was_supplied : given.a_retry_budget
{
    CancellationToken _token;

    void Because()
    {
        using var lifetime = _retries.Bind(_context);
        _token = lifetime.Token;
    }

    [Fact] void should_preserve_the_existing_activation_token() => _token.ShouldEqual(_activation.Token);
}
