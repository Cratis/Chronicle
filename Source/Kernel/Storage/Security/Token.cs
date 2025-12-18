// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents an OAuth token.
/// </summary>
/// <param name="Id">The unique identifier for the token.</param>
/// <param name="ApplicationId">The application identifier.</param>
/// <param name="AuthorizationId">The authorization identifier.</param>
/// <param name="Subject">The subject (user identifier).</param>
/// <param name="Type">The token type.</param>
/// <param name="Status">The token status.</param>
/// <param name="Payload">The token payload.</param>
/// <param name="ReferenceId">The reference identifier.</param>
/// <param name="CreationDate">When the token was created.</param>
/// <param name="ExpirationDate">When the token expires.</param>
/// <param name="RedemptionDate">When the token was redeemed.</param>
/// <param name="Properties">Additional properties.</param>
public record Token(
    string Id,
    string? ApplicationId,
    string? AuthorizationId,
    string? Subject,
    string? Type,
    string? Status,
    string? Payload,
    string? ReferenceId,
    DateTimeOffset? CreationDate,
    DateTimeOffset? ExpirationDate,
    DateTimeOffset? RedemptionDate,
    ImmutableDictionary<string, JsonElement> Properties);
