// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The exception that is thrown when a custom observer registry cannot perform a new type-based operation.
/// </summary>
/// <param name="providerType">The custom registry type.</param>
public class TypeBasedArtifactRegistrationNotSupported(Type providerType)
    : Exception($"'{providerType.FullName}' does not support the new type-based artifact operation. Implement its type-based core to support explicitly registered observers.");
