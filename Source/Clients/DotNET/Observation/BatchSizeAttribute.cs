// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Declares the bounds of the pages a reactor or reducer receives, and how many event sources of a reducer's page are reduced at once.
/// </summary>
/// <remarks>
/// Every value left at zero keeps its default. An observer with only per-event methods receives pages of up to 100 events, an
/// observer with a batch method pages of up to 500 events, and neither waits to fill a page. The kernel caps every bound with
/// its own maximums, so a page may be smaller than declared.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BatchSizeAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the most events a page holds. Zero keeps the default.
    /// </summary>
    public int MaxEvents { get; set; }

    /// <summary>
    /// Gets or sets the most bytes of event content a page holds. Zero leaves it to the kernel.
    /// </summary>
    public int MaxBytes { get; set; }

    /// <summary>
    /// Gets or sets how long, in milliseconds, the kernel may wait to fill a live page. Zero sends what is waiting.
    /// </summary>
    public int MaxDelayMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets how many event sources of one page a reducer reduces at the same time. Zero keeps the default,
    /// <see cref="Environment.ProcessorCount"/>; one reduces them one after another. Reactors ignore it.
    /// </summary>
    public int MaxConcurrency { get; set; }
}
