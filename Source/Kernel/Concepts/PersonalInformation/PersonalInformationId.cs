// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Concepts.Compliance.PersonalInformation;

/// <summary>
/// Represents the unique identifier of a <see cref="PersonalInformationValue"/>.
/// </summary>
/// <param name="Value">The actual string value.</param>
public record PersonalInformationId(Guid Value) : ConceptAs<Guid>(Value);
