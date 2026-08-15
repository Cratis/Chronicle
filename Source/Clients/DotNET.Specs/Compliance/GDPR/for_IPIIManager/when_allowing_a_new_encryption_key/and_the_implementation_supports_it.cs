// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR.for_IPIIManager.when_allowing_a_new_encryption_key;

/// <summary>
/// The other half of the default implementation: an implementation that overrides the member is used, and the
/// default never gets in its way.
/// </summary>
public class and_the_implementation_supports_it : given.two_implementations
{
    supports_the_member _implementation;
    IPIIManager _manager;
    Exception _error;

    void Establish()
    {
        _implementation = new supports_the_member();
        _manager = _implementation;
    }

    async Task Because() => _error = await Catch.Exception(() => _manager.AllowNewEncryptionKeyFor(Identifier));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_authorize_a_new_key_for_the_subject() => _implementation.AuthorizedFor.ShouldEqual(Identifier);
}
