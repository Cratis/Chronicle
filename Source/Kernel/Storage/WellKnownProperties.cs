// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Changes;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Extension methods for <see cref="IChangeset{TSource, TTarget}"/>.
/// </summary>
public static class WellKnownProperties
{
    /// <summary>
    /// The property name for the last handled sequence number.
    /// </summary>
    public const string LastHandledEventSequenceNumber = "__lastHandledEventSequenceNumber";

    /// <summary>
    /// The property name for whether an model instance is initialized.
    /// </summary>
    public const string ReadModelInstanceInitialized = "__initialized";

    /// <summary>
    /// The property name for the subject (compliance identity target) of the event.
    /// </summary>
    public const string Subject = "__subject";

    /// <summary>
    /// All the kernel bookkeeping property names.
    /// </summary>
    /// <remarks>
    /// The kernel stamps these onto a read model document itself — they are not read model data. Anything that
    /// walks a document against the read model's schema has to account for them, because a read model does not
    /// declare them unless it deliberately exposes one. Every name declared above belongs in here; leaving one
    /// out is how such a walk ends up rejecting a property the kernel put there.
    /// </remarks>
    public static readonly ImmutableArray<string> All = [LastHandledEventSequenceNumber, ReadModelInstanceInitialized, Subject];
}
