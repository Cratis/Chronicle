// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Represents how a client retries registering its artifacts when the kernel does not accept them.
/// </summary>
/// <remarks>
/// Registering is what a host does on its way up, so a registration that fails takes the host with it. A kernel that
/// is merely busy - catching up a newly added read model over a large event log, say - answers late rather than
/// wrongly, and a host that dies on that answer comes back and asks again, adding its whole registration to the queue
/// it was already waiting on. Retrying in place instead lets the client wait out a busy kernel. Registration is
/// idempotent, so a retry costs the kernel nothing beyond the comparison it already does; a kernel that is genuinely
/// wrong keeps failing and the host still ends up down, just after the attempts are spent rather than on the first.
/// </remarks>
public class RegistrationRetryOptions
{
    /// <summary>
    /// Gets or sets how many times registering all artifacts is attempted before the failure is surfaced. One
    /// disables retrying. Defaults to five.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets how long to wait after the first failed attempt. Each further attempt doubles it, jittered, up
    /// to <see cref="MaximumDelay"/>. Defaults to two seconds.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the upper bound for the wait between attempts. Defaults to thirty seconds.
    /// </summary>
    public TimeSpan MaximumDelay { get; set; } = TimeSpan.FromSeconds(30);
}
