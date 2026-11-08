// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the effective bounds of the pages handed to a client observer that declared page delivery.
/// </summary>
/// <param name="MaxEvents">The most events in one page.</param>
/// <param name="MaxBytes">The most bytes of event content in one page.</param>
/// <remarks>
/// An event larger than <paramref name="MaxBytes"/> still goes out, alone in its page, so a bound never stalls a partition.
/// </remarks>
public record PageBounds(int MaxEvents, int MaxBytes)
{
    /// <summary>
    /// Gets bounds that never split a page.
    /// </summary>
    public static readonly PageBounds Unbounded = new(int.MaxValue, int.MaxValue);

    /// <summary>
    /// Gets a value indicating whether the bounds can split a page.
    /// </summary>
    public bool IsBounded => MaxEvents != int.MaxValue || MaxBytes != int.MaxValue;

    /// <summary>
    /// Gets whether the size of each event has to be measured to honor the bounds.
    /// </summary>
    public bool MeasuresBytes => MaxBytes != int.MaxValue;

    /// <summary>
    /// Gets the effective bounds for what a client declared, capped by the kernel's own maximums.
    /// </summary>
    /// <param name="declared">The <see cref="PageDeliveryDefinition"/> the client declared, or null when it declared no page delivery.</param>
    /// <param name="configuration">The <see cref="Observers"/> configuration holding the kernel's maximums.</param>
    /// <returns>The effective <see cref="PageBounds"/>; <see cref="Unbounded"/> when the client declared no page delivery.</returns>
    /// <remarks>
    /// A bound the client declared above zero is used up to the kernel's maximum; zero takes the kernel's maximum.
    /// The kernel's maximums are <see cref="Observers.MaxEventsPerSubscriberCall"/> and <see cref="Observers.MaxBytesPerSubscriberCall"/>,
    /// where zero or less means the kernel sets no maximum.
    /// </remarks>
    public static PageBounds For(PageDeliveryDefinition? declared, Observers configuration)
    {
        if (declared is null)
        {
            return Unbounded;
        }

        return new(
            Cap(declared.MaxEventsPerPage, configuration.MaxEventsPerSubscriberCall),
            Cap(declared.MaxBytesPerPage, configuration.MaxBytesPerSubscriberCall));
    }

    static int Cap(int declared, int kernelMaximum)
    {
        var maximum = kernelMaximum > 0 ? kernelMaximum : int.MaxValue;
        return declared > 0 ? Math.Min(declared, maximum) : maximum;
    }
}
