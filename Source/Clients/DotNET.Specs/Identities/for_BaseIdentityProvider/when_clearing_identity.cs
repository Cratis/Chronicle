// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities.for_BaseIdentityProvider;

public class when_clearing_identity : Specification
{
    IIdentityProvider provider;
    Identity original_identity;
    Identity retrieved_identity;

    void Establish()
    {
        provider = new BaseIdentityProvider();
        original_identity = new("test-subject", "Test User", "testuser");
        provider.SetCurrentIdentity(original_identity);
    }

    void Because()
    {
        provider.ClearCurrentIdentity();
        retrieved_identity = provider.GetCurrent();
    }

    [Fact] void should_return_system_identity() => retrieved_identity.ShouldEqual(Identity.System);
}
