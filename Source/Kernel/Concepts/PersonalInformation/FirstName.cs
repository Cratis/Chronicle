// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance.GDPR;

namespace Aksio.Cratis.Kernel.Concepts.Compliance.PersonalInformation;

public record FirstName(string Value) : PIIConceptAs<string>(Value);
