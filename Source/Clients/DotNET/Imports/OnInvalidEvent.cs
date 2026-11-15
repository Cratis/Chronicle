// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents what an import does when one of its events is invalid.
/// </summary>
public enum OnInvalidEvent
{
    /// <summary>Fail the whole import and commit nothing. The default.</summary>
    Reject = 0,

    /// <summary>Leave the invalid events out, report them, and commit the rest.</summary>
    Skip = 1
}
