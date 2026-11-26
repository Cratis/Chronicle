// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Registrations.for_RegistrationInbox.given;

/// <summary>
/// Remembers the definitions by an id it writes as the payload, standing in for the serializer.
/// </summary>
public class FakeRegistrationPayloads : IRegistrationPayloads
{
    readonly Dictionary<string, IReadOnlyList<ProjectionDefinition>> _definitions = [];

    public byte[] Serialize(IReadOnlyList<ProjectionDefinition> definitions)
    {
        var id = Guid.NewGuid().ToString();
        _definitions[id] = definitions;
        return Encoding.UTF8.GetBytes(id);
    }

    public IReadOnlyList<ProjectionDefinition> Deserialize(ReadOnlyMemory<byte> payload) => _definitions[Encoding.UTF8.GetString(payload.Span)];
}
