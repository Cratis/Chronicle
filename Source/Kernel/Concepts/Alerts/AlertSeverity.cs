// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Alerts;

/// <summary>
/// Represents how urgent an alert is.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Something is wrong and worth looking at, but Chronicle is still working on it.
    /// </summary>
    Warning = 0,

    /// <summary>
    /// Chronicle has stopped working on it and a person has to act.
    /// </summary>
    Critical = 1
}
