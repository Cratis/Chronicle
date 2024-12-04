// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Workers;

/// <summary>
/// The type of the error that occurred while performing work step.
/// </summary>
public enum PerformWorkError
{
    /// <summary>
    /// Unknown error occurred.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The work was cancelled.
    /// </summary>
    Cancelled = 1
}
