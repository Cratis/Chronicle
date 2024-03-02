// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Concepts.Compliance.PersonalInformation;

public record PersonalInformationId(Guid Value) : ConceptAs<Guid>(Value);
