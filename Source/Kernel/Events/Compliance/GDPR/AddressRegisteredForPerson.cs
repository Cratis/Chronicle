// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.PersonalInformation;

namespace Cratis.Kernel.Compliance.GDPR.Events;

/// <summary>
/// Represents the event that gets applied when an address is registered for a person.
/// </summary>
/// <param name="Address"><see cref="Address"/> as part of the whole address, typically street name and number, apartment or more.</param>
/// <param name="City"><see cref="City"/> as part of the address.</param>
/// <param name="PostalCode"><see cref="PostalCode"/> as part of the address.</param>
/// <param name="Country"><see cref="Country"/> as part of the address.</param>
public record AddressRegisteredForPerson(Address Address, City City, PostalCode PostalCode, Country Country);
