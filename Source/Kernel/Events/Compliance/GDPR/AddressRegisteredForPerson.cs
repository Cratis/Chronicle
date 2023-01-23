// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Concepts.Compliance.PersonalInformation;

namespace Aksio.Cratis.Kernel.Compliance.GDPR.Events;

[EventType("fc5a389b-efd9-4606-9532-86c08cb10946")]
public record AddressRegisteredForPerson(Address Address, City City, PostalCode PostalCode, Country Country);
