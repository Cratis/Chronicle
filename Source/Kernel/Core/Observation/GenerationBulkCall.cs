// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// What a handler call does to the bulk window of an observer.
/// </summary>
internal enum GenerationBulkCall
{
    /// <summary>Uses the open window without capturing one.</summary>
    Use = 0,

    /// <summary>Begins a window, always capturing a new grant.</summary>
    Begin = 1,

    /// <summary>Resumes the open window, capturing one only when none is open.</summary>
    Resume = 2
}
