// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Registrations;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The answer the kernel gives when it accepts a registration.
/// </summary>
/// <param name="Revision">The revision the registration was accepted as.</param>
/// <param name="State">The <see cref="RegistrationIntentState"/> of the intent: pending until it has been applied, applied when it repeats what was applied.</param>
[GenerateSerializer]
public record RegistrationAcceptance(
    [property: Id(0)] long Revision,
    [property: Id(1)] RegistrationIntentState State);
