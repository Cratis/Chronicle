// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Defines how the content of an accepted registration is written to, and read back from, its durable intent.
/// </summary>
public interface IRegistrationPayloads
{
    /// <summary>
    /// Serializes projection definitions.
    /// </summary>
    /// <param name="definitions">The definitions.</param>
    /// <returns>The bytes to store.</returns>
    byte[] Serialize(IReadOnlyList<ProjectionDefinition> definitions);

    /// <summary>
    /// Reads projection definitions back.
    /// </summary>
    /// <param name="payload">The stored bytes.</param>
    /// <returns>The definitions.</returns>
    IReadOnlyList<ProjectionDefinition> Deserialize(ReadOnlyMemory<byte> payload);
}
