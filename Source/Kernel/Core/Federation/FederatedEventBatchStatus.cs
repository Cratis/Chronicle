// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The outcome of a pull.
/// </summary>
public static class FederatedEventBatchStatus
{
    /// <summary>
    /// Events (possibly none) are returned.
    /// </summary>
    public const string Ok = "Ok";

    /// <summary>
    /// The caller is not an authenticated, active link, or is not granted what it asked for.
    /// </summary>
    public const string Denied = "Denied";

    /// <summary>
    /// The link exceeded its rate limit. Retry later.
    /// </summary>
    public const string Throttled = "Throttled";
}
