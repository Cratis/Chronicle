// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities.for_IdentityConverters;

public class when_round_tripping_identity_conversion : given.an_identity
{
    Identity _result;

    void Because()
    {
        var contract = _identity.ToContract();
        _result = contract.ToClient();
    }

    [Fact] void should_preserve_subject() => _result.Subject.ShouldEqual(_identity.Subject);
    [Fact] void should_preserve_name() => _result.Name.ShouldEqual(_identity.Name);
    [Fact] void should_preserve_username() => _result.UserName.ShouldEqual(_identity.UserName);
    [Fact] void should_preserve_on_behalf_of_subject() => _result.OnBehalfOf.Subject.ShouldEqual(_identity.OnBehalfOf.Subject);
    [Fact] void should_preserve_on_behalf_of_name() => _result.OnBehalfOf.Name.ShouldEqual(_identity.OnBehalfOf.Name);
    [Fact] void should_preserve_on_behalf_of_username() => _result.OnBehalfOf.UserName.ShouldEqual(_identity.OnBehalfOf.UserName);
}
