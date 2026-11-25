// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.DependencyInjection;
using Orleans.Serialization;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Writes registrations with the serializer the silos use between each other, which already knows every type a
/// registration is made of.
/// </summary>
/// <param name="serializer">The <see cref="Serializer{T}"/> for projection definitions.</param>
[Singleton]
public class RegistrationPayloads(Serializer<ProjectionDefinition[]> serializer) : IRegistrationPayloads
{
    /// <inheritdoc/>
    public byte[] Serialize(IReadOnlyList<ProjectionDefinition> definitions) => serializer.SerializeToArray([.. definitions]);

    /// <inheritdoc/>
    public IReadOnlyList<ProjectionDefinition> Deserialize(ReadOnlyMemory<byte> payload) => serializer.Deserialize(payload.Span) ?? [];
}
