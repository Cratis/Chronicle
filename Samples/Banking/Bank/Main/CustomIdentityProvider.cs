// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Identities;

namespace Main;

public class CustomIdentityProvider : IIdentityProvider
{
    static readonly Identity _identity = new("1", "Super", "Admin", new("2", "Regular", "User"));

    public Identity GetCurrent() => _identity;
}
