// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public const string LasHandledEventSequenceNumber = "__lastHandledEventSequenceNumber";

    /// <summary>
    /// The property name for whether an model instance is initialized.
    /// </summary>
    public const string ModelInstanceInitialized = "__initialized";
}
