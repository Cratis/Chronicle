// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Defines the code-owned reason a token endpoint could not be used. Replaces exception text in diagnostics.
/// </summary>
internal enum TokenFailureCategory
{
    /// <summary>
    /// The endpoint could not be reached.
    /// </summary>
    Unreachable = 0,

    /// <summary>
    /// The endpoint did not answer in time.
    /// </summary>
    Timeout = 1,

    /// <summary>
    /// The endpoint answered with something that could not be used.
    /// </summary>
    InvalidResponse = 2,

    /// <summary>
    /// The endpoint answered with a non-success status or a redirect.
    /// </summary>
    Rejected = 3
}
