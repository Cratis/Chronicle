// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Compliance.GDPR;

namespace Aksio.Cratis.Compliance.Concepts.PersonalInformation;

public record PersonalInformationValue(string Value) : PIIConceptAs<string>(Value);
