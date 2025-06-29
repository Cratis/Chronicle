// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities.for_BaseIdentityProvider;

public class when_clearing_identity : Specification
{
    void Establish()
    {
        BaseIdentityProvider.SetCurrentIdentity(new Identity(
            Guid.NewGuid().ToString(),
            "Name",
            "UserName",
            null));
        BaseIdentityProvider.ClearCurrentIdentity();
    }

    [Fact] void should_return_system_identity() => ((IIdentityProvider)new BaseIdentityProvider()).GetCurrent().ShouldEqual(Identity.System);
}
