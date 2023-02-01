// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

#pragma warning disable RCS1241 // Implement IComparer

/// <summary>
/// Represents a comparer for <see cref="AppendedEvent"/> by sequence number.
/// </summary>
public class AppendedEventComparer : IComparer<AppendedEvent>
{
    /// <inheritdoc/>
    public int Compare(AppendedEvent? x, AppendedEvent? y) =>
        Comparer<ulong>.Default.Compare(x?.Metadata.SequenceNumber.Value ?? ulong.MaxValue, y?.Metadata.SequenceNumber.Value ?? ulong.MaxValue);
}
