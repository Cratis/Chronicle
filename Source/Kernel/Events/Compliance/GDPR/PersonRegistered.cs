// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.PersonalInformation;

namespace Cratis.Chronicle.Compliance.GDPR.Events;

/// <summary>
/// Represents the event that gets applied when a person is registered.
/// </summary>
/// <param name="FirstName"><see cref="FirstName"/> of the person.</param>
/// <param name="LastName"><see cref="LastName"/> of the person.</param>
/// <param name="SocialSecurityNumber"><see cref="SocialSecurityNumber"/> for the person.</param>
public record PersonRegistered(FirstName FirstName, LastName LastName, SocialSecurityNumber SocialSecurityNumber);
