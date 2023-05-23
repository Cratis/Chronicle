// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Compliance.GDPR;

namespace Aksio.Cratis.Kernel.Concepts.Compliance.PersonalInformation;

public record Address(string Value) : PIIConceptAs<string>(Value);
