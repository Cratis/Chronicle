// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Concepts.Compliance.PersonalInformation;
using Cratis.Chronicle.Events;

namespace Aksio.Cratis.Kernel.Compliance.GDPR.Events;

[EventType("e70bdf4e-d757-4241-b59a-74d61b0ffd49")]
public record PersonalInformationRegistered(PersonalInformationId Identifier, PersonalInformationType Type, PersonalInformationValue Value);
