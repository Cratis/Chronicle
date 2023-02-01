// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

#pragma warning disable RCS1241 // Implement IComparer

/// <summary>
/// Represents a comparer for <see cref="AppendedEventByDate"/> by date.
/// </summary>
public class AppendedEventByDateComparer : IComparer<AppendedEventByDate>
{
    /// <inheritdoc/>
    public int Compare(AppendedEventByDate? x, AppendedEventByDate? y) =>
        Comparer<DateTimeOffset>.Default.Compare(x?.DateTime ?? DateTimeOffset.MaxValue, y?.DateTime ?? DateTimeOffset.MaxValue);
}
