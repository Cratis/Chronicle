// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.EventSequences;

#pragma warning disable RCS1241 // Implement IComparer

/// <summary>
/// Represents a comparer for <see cref="AppendedEvent"/> by sequence number.
/// </summary>
public class AppendedEventComparer : IComparer<AppendedEventWithSequenceNumber>
{
    /// <inheritdoc/>
    public int Compare(AppendedEventWithSequenceNumber? x, AppendedEventWithSequenceNumber? y) =>
        Comparer<ulong>.Default.Compare(x?.SequenceNumber.Value ?? ulong.MaxValue, y?.SequenceNumber.Value ?? ulong.MaxValue);
}
