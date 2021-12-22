// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dolittle.SDK.Events;

namespace Sample.Customers
{
    [EventType("5e07940e-7fd0-4b26-beaf-606e8917dd57")]
    public record CustomerSignedUp(SocialSecurityNumber SocialSecurityNumber, FirstName FirstName, LastName LastName);
}
