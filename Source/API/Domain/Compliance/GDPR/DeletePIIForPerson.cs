// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Concepts.Compliance.PersonalInformation;

namespace Cratis.Kernel.Domain.Compliance.GDPR;

public record DeletePIIForPerson(PersonId PersonId);
