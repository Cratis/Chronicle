// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities.for_BaseIdentityProvider;

public class when_setting_and_getting_identity : Specification
{
    IIdentityProvider provider;
    Identity original_identity;
    Identity retrieved_identity;

    void Establish()
    {
        provider = new BaseIdentityProvider();
        original_identity = new("test-subject", "Test User", "testuser");
    }

    void Because()
    {
        provider.SetCurrentIdentity(original_identity);
        retrieved_identity = provider.GetCurrent();
    }

    [Fact] void should_return_the_set_identity() => retrieved_identity.ShouldEqual(original_identity);
}
