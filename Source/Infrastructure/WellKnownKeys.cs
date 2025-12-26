// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle;

/// <summary>
/// Holds constants.
/// </summary>
public static class WellKnownKeys
{
    /// <summary>
    /// The key for the correlation id.
    /// </summary>
    public const string CorrelationId = "x-correlation-id";

    /// <summary>
    /// The key for the user identity (subject claim).
    /// </summary>
    public const string UserIdentity = "x-user-identity";

    /// <summary>
    /// The key for the user identity (subject claim).
    /// </summary>
    public const string UserName = "x-user-name";

    /// <summary>
    /// The key for the user identity (subject claim).
    /// </summary>
    public const string UserPreferredUserName = "x-user-preferred-username";
}
