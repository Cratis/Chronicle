// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities.for_IdentityConverters.given;

public class an_identity : Specification
{
    protected Identity _identity;

    void Establish()
    {
        _identity = new Identity(
            Guid.NewGuid().ToString(),
            "Name",
            "UserName",
            new Identity(Guid.NewGuid().ToString(), "OnBehalfOfName", "OnBehalfOfUserName", null));
    }
}
