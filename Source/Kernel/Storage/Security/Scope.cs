// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents an OAuth scope.
/// </summary>
/// <param name="Id">The unique identifier for the scope.</param>
/// <param name="Name">The scope name.</param>
/// <param name="DisplayName">The display name of the scope.</param>
/// <param name="Description">The scope description.</param>
/// <param name="Resources">The resources associated with the scope.</param>
/// <param name="Properties">Additional properties.</param>
public record Scope(
    string Id,
    string? Name,
    string? DisplayName,
    string? Description,
    ImmutableArray<string> Resources,
    ImmutableDictionary<string, JsonElement> Properties);
