// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Webhooks;

/// <summary>
/// Represents the result of testing OAuth authorization.
/// </summary>
public class TestOAuthAuthorizationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the authorization was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the authorization failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}
