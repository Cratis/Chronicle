// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance.Concepts.PersonalInformation;

namespace Aksio.Cratis.Compliance.Domain.GDPR;

public record DeletePIIForPerson(PersonId PersonId);
