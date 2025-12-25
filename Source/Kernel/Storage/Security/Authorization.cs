// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Security;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

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
    AuthorizationId Id,
    ApplicationId? ApplicationId,
    Subject? Subject,
    AuthorizationType? Type,
    AuthorizationStatus? Status,
    ImmutableArray<Scope> Scopes,
    DateTimeOffset? CreationDate,
    ImmutableDictionary<PropertyName, JsonElement> Properties);
