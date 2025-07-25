// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities.for_BaseIdentityProvider;

public class when_clearing_identity : Specification
{
    IIdentityProvider _provider;
    Identity _originalIdentity;
    Identity _retrievedIdentity;

    void Establish()
    {
        _provider = new BaseIdentityProvider();
        _originalIdentity = new("test-subject", "Test User", "testuser");
        _provider.SetCurrentIdentity(_originalIdentity);
    }

    void Because()
    {
        _provider.ClearCurrentIdentity();
        _retrievedIdentity = _provider.GetCurrent();
    }

    [Fact] void should_return_system_identity() => _retrievedIdentity.ShouldEqual(Identity.System);
}
