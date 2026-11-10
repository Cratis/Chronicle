// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Declares the bounds of the pages a reactor or reducer receives, and how many event sources of a reducer's page are reduced at once.
/// </summary>
/// <remarks>
/// <para>
/// The client sends the bounds when it registers the observer, and the kernel honors <see cref="MaxEvents"/> and
/// <see cref="MaxBytes"/> on every path: live, catch-up, replay and retry. It caps each with its own maximum, the
/// maxEventsPerSubscriberCall and maxBytesPerSubscriberCall observer settings (100 events and 4 MiB by
/// default), so a page may be smaller than declared.
/// </para>
/// <para>
/// A value left at zero keeps its default. The client sends 100 events for an observer with only per-event methods and
/// 500 for an observer with a batch method, and zero bytes, which leaves the byte bound to the kernel's maximum.
/// <see cref="MaxDelayMilliseconds"/> is sent but not applied by the kernel yet, so a live page holds what is already waiting.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class BatchSizeAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the most events a page holds, capped by the kernel's maximum. Zero keeps the default: 100 for an
    /// observer with only per-event methods, 500 for an observer with a batch method.
    /// </summary>
    public int MaxEvents { get; set; }

    /// <summary>
    /// Gets or sets the most bytes of event content a page holds, counted as the UTF-8 length of each event's JSON content
    /// and capped by the kernel's maximum. Zero leaves the bound to the kernel's maximum.
    /// </summary>
    public int MaxBytes { get; set; }

    /// <summary>
    /// Gets or sets how long, in milliseconds, the kernel may wait to fill a live page. The client sends it, but the kernel
    /// does not apply it yet: a live page holds what is already waiting, whatever the value.
    /// </summary>
    public int MaxDelayMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets how many event sources of one page a reducer reduces at the same time. Zero keeps the default,
    /// <see cref="Environment.ProcessorCount"/>; one reduces them one after another. Reactors ignore it.
    /// </summary>
    public int MaxConcurrency { get; set; }
}
