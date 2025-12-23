// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents an OAuth authorization.
/// </summary>
/// <param name="Id">The unique identifier for the authorization.</param>
/// <param name="ApplicationId">The application identifier.</param>
/// <param name="Subject">The subject (user identifier).</param>
/// <param name="Type">The authorization type.</param>
/// <param name="Status">The authorization status.</param>
/// <param name="Scopes">The scopes granted.</param>
/// <param name="CreationDate">When the authorization was created.</param>
/// <param name="Properties">Additional properties.</param>
public record Authorization(
    string Id,
    string? ApplicationId,
    string? Subject,
    string? Type,
    string? Status,
    ImmutableArray<string> Scopes,
    DateTimeOffset? CreationDate,
    ImmutableDictionary<string, JsonElement> Properties);
