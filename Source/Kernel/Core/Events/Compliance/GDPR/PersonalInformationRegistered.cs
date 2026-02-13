// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.PersonalInformation;

namespace Cratis.Chronicle.Compliance.GDPR.Events;

/// <summary>
/// Represents the event that gets applied when personal information is registered.
/// </summary>
/// <param name="Identifier">The unique <see cref="PersonalInformationId"/> of the information.</param>
/// <param name="Type"><see cref="PersonalInformationType"/> to register.</param>
/// <param name="Value">The actual <see cref="PersonalInformationValue">value</see>.</param>
public record PersonalInformationRegistered(PersonalInformationId Identifier, PersonalInformationType Type, PersonalInformationValue Value);
