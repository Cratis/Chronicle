// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for how the kernel takes in registrations from clients.
/// </summary>
public class Registration
{
    /// <summary>
    /// Gets a value indicating whether the kernel accepts registrations durably and applies them in the background
    /// instead of applying them before it answers. The default is true.
    /// </summary>
    public bool AcceptAsynchronously { get; init; } = true;

    /// <summary>
    /// Gets how many distinct registrations one registering grain applies at the same time. Registrations beyond the
    /// limit wait in an ordered queue instead of starting. The default is 4.
    /// </summary>
    public int MaxConcurrentRegistrations { get; init; } = 4;

    /// <summary>
    /// Gets how long a registration may wait in the queue, in seconds, before it is answered with a retryable "busy"
    /// response instead of waiting for the caller's call timeout. The default is 20, below the 30 second call timeout.
    /// </summary>
    public int MaxQueueAgeSeconds { get; init; } = 20;

    /// <summary>
    /// Gets the number of seconds a busy response tells the caller to wait before it retries. The default is 5.
    /// </summary>
    public int BusyRetryAfterSeconds { get; init; } = 5;

    /// <summary>
    /// Gets how many times an accepted registration is applied before it is recorded as failed. The default is 3.
    /// </summary>
    public int MaxApplyAttempts { get; init; } = 3;

    /// <summary>
    /// Gets the number of seconds to wait between attempts to apply an accepted registration. The default is 5.
    /// </summary>
    public int ApplyRetryDelaySeconds { get; init; } = 5;
}
