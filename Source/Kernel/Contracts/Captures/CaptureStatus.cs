// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Captures;

/// <summary>
/// Represents the lifecycle status of a capture.
/// </summary>
public enum CaptureStatus
{
    /// <summary>
    /// The capture is stopped and can be edited.
    /// </summary>
    Stopped = 0,

    /// <summary>
    /// The capture is started and actively capturing - it can not be edited until stopped.
    /// </summary>
    Started = 1
}
