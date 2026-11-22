// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Namespaces;

/// <summary>A case-folded owner address; JSON preserves legacy names containing delimiters.</summary>
/// <param name="EventStore">The canonical event store.</param>
/// <param name="Namespace">The namespace name.</param>
public record NamespaceIncarnationKey(string EventStore, string Namespace)
{
    /// <inheritdoc/>
    public override string ToString() => JsonSerializer.Serialize(this with { Namespace = Namespace.ToUpperInvariant() });
}
