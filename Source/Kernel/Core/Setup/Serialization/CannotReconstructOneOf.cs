// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization;

/// <summary>
/// The exception that is thrown when a OneOf type cannot be reconstructed during deserialization.
/// </summary>
/// <param name="type">The type that could not be reconstructed.</param>
public class CannotReconstructOneOf(Type type)
    : Exception($"Unable to reconstruct OneOf type '{type.FullName}' during deserialization. It must be a OneOf<...> or expose a constructor taking the underlying OneOf<...> value.");
