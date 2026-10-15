// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Occurrences;

/// <summary>
/// The recorded effect outcome.
/// </summary>
public enum EffectOutcome
{
    /// <summary>
    /// Appended.
    /// </summary>
    Appended = 0,

    /// <summary>
    /// NoEvents.
    /// </summary>
    NoEvents = 1,

    /// <summary>
    /// Refused.
    /// </summary>
    Refused = 2
}
