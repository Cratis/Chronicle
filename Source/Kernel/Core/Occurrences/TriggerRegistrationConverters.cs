// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Converts the generated trigger contract to the kernel registration.
/// </summary>
internal static class TriggerRegistrationConverters
{
    /// <summary>
    /// Converts the plaintext payload contract without changing its schema.
    /// </summary>
    /// <param name="registration">The generated wire registration.</param>
    /// <returns>The kernel registration.</returns>
    public static TriggerRegistration ToApi(this Contracts.Occurrences.TriggerRegistration registration) => new(registration.Id, registration.Name, registration.Schema);
}
