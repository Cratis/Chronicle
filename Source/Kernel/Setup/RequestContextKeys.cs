// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup;

/// <summary>
/// Holds keys for the <see cref="RequestContext"/>.
/// </summary>
public static class RequestContextKeys
{
    /// <summary>
    /// Gets the key for the correlation id in the <see cref="RequestContext"/>.
    /// </summary>
    public const string CorrelationIdKey = "CorrelationId";
}
