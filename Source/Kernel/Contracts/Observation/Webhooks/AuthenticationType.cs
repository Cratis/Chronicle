// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Observation.Webhooks;

/// <summary>
/// Represents the type of authentication supported by a webhook.
/// </summary>
public enum AuthenticationType
{
    /// <summary>
    /// No authentication.
    /// </summary>
    None = 0,

    /// <summary>
    /// Basic authentication.
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Bearer authentication.
    /// </summary>
    Bearer = 2
}