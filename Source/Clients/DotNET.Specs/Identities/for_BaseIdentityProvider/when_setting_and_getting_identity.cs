// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities.for_BaseIdentityProvider;

public class when_setting_and_getting_identity : Specification
{
    Identity _identity;
    Identity _result;

    void Establish()
    {
        _identity = new Identity(
            Guid.NewGuid().ToString(),
            "Name",
            "UserName",
            null);
    }

    void Because()
    {
        BaseIdentityProvider.SetCurrentIdentity(_identity);
        _result = ((IIdentityProvider)new BaseIdentityProvider()).GetCurrent();
    }

    [Fact] void should_return_the_set_identity() => _result.ShouldEqual(_identity);
}
