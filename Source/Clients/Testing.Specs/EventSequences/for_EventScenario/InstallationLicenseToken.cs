// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A license token for the whole Chronicle installation - global-scoped rather than subject- or namespace-scoped,
/// because it is not tied to any one event store, namespace, or partner's identity.
/// </summary>
/// <param name="Value">The license token.</param>
[Encrypted(EncryptionScope.Global)]
public record InstallationLicenseToken(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="InstallationLicenseToken"/>.
    /// </summary>
    /// <param name="value">The license token.</param>
    public static implicit operator InstallationLicenseToken(string value) => new(value);
}
