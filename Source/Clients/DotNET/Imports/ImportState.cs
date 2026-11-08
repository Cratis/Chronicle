// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents the state of an import session.
/// </summary>
public enum ImportState
{
    /// <summary>The import is open and has not received any chunk yet.</summary>
    Open = 0,

    /// <summary>The import has received chunks and accepts more.</summary>
    Staging = 1,

    /// <summary>Staging is closed and the whole import is being validated.</summary>
    Validating = 2,

    /// <summary>The staged events are being committed to the event sequence.</summary>
    Committing = 3,

    /// <summary>Every staged event was committed or reported.</summary>
    Completed = 4,

    /// <summary>The import failed; when it failed before committing, nothing was committed.</summary>
    Failed = 5,

    /// <summary>The import was abandoned before its commit started.</summary>
    Abandoned = 6
}
