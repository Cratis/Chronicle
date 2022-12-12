// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Aksio.Cratis.Applications.Identity;

/// <summary>
/// Represents the context used when providing identity payload.
/// </summary>
/// <param name="Id">Unique identifier for the identity.</param>
/// <param name="Name">Name of the identity.</param>
/// <param name="Token">The raw token as JSON.</param>
/// <param name="Claims">Any claims.</param>
public record IdentityProviderContext(IdentityId Id, IdentityName Name, JsonObject Token, IEnumerable<KeyValuePair<string, string>> Claims);
