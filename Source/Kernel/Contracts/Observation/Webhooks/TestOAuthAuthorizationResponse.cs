// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Represents the response for testing OAuth authorization.
/// </summary>
[ProtoContract]
public class TestOAuthAuthorizationResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the authorization was successful.
    /// </summary>
    [ProtoMember(1)]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the authorization failed.
    /// </summary>
    [ProtoMember(2)]
    public string ErrorMessage { get; set; } = string.Empty;
}
