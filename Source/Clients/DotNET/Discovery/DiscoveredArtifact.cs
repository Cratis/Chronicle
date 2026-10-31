// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// Represents an artifact found by the client, not a registration verdict.
/// </summary>
/// <param name="Kind">The kind of artifact.</param>
/// <param name="Id">The identifier used when registering it.</param>
/// <param name="ClrType">The declared CLR type, or null for an artifact without one.</param>
public record DiscoveredArtifact(ArtifactKind Kind, string Id, Type? ClrType);
