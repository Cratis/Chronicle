// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Security;

/// <summary>
/// Represents bearer token authentication for a webhook.
/// </summary>
public class BearerTokenAuthorization
{
    /// <summary>
    /// Gets or sets the bearer token.
    /// </summary>
    public string Token { get; set; } = string.Empty;
}
