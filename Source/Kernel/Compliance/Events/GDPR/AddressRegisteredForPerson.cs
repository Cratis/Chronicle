// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance.Concepts.PersonalInformation;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Compliance.Events
{
    [EventType("fc5a389b-efd9-4606-9532-86c08cb10946")]
    public record AddressRegisteredForPerson(Address Address, City City, PostalCode PostalCode, Country Country);
}
