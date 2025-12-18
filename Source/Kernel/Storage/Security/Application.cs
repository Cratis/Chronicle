// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents an OAuth application.
/// </summary>
/// <param name="Id">The unique identifier for the application.</param>
/// <param name="ClientId">The client identifier.</param>
/// <param name="ClientSecret">The hashed client secret.</param>
/// <param name="DisplayName">The display name of the application.</param>
/// <param name="Type">The client type (e.g., confidential, public).</param>
/// <param name="ConsentType">The consent type (e.g., explicit, implicit).</param>
/// <param name="Permissions">The permissions granted to the application.</param>
/// <param name="Requirements">The requirements for the application.</param>
/// <param name="RedirectUris">The redirect URIs.</param>
/// <param name="PostLogoutRedirectUris">The post-logout redirect URIs.</param>
/// <param name="Properties">Additional properties.</param>
public record Application(
    string Id,
    string ClientId,
    string? ClientSecret,
    string? DisplayName,
    string? Type,
    string? ConsentType,
    ImmutableArray<string> Permissions,
    ImmutableArray<string> Requirements,
    ImmutableArray<string> RedirectUris,
    ImmutableArray<string> PostLogoutRedirectUris,
    ImmutableDictionary<string, JsonElement> Properties);
