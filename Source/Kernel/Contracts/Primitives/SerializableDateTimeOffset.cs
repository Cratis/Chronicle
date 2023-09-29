// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf;

namespace Aksio.Cratis.Kernel.Contracts.Primitives;

/// <summary>
/// Proto contract that represents a <see cref="DateTimeOffset"/>.
/// </summary>
/// <remarks>
/// Based on implementation found here: https://stackoverflow.com/a/68572913/26049.
/// </remarks>
[ProtoContract]
public class SerializableDateTimeOffset
{
    /// <summary>
    /// Gets or sets the Utc ticks.
    /// </summary>
    [ProtoMember(1)]
    public long Ticks { get; set; }

    /// <summary>
    /// Gets or sets the UTC offset in minutes.
    /// </summary>
    [ProtoMember(2)]
    public double OffsetMinutes { get; set; }

    /// <summary>
    /// Operator to cast <see cref="SerializableDateTimeOffset"/> to <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="protoDateTimeOffset"><see cref="SerializableDateTimeOffset"/> to convert from.</param>
    /// <returns>A converted <see cref="DateTimeOffset"/>.</returns>
    public static implicit operator DateTimeOffset?(SerializableDateTimeOffset? protoDateTimeOffset)
    {
        if (protoDateTimeOffset == null)
        {
            return null;
        }

        return new DateTimeOffset(protoDateTimeOffset.Ticks, TimeSpan.FromMinutes(protoDateTimeOffset.OffsetMinutes));
    }

    /// <summary>
    /// Implicitly convert <see cref="DateTimeOffset"/> to <see cref="SerializableDateTimeOffset"/>.
    /// </summary>
    /// <param name="protoDateTimeOffset"><see cref="SerializableDateTimeOffset"/> to convert from.</param>
    /// <returns>A converted <see cref="DateTimeOffset"/>.</returns>
    public static implicit operator DateTimeOffset(SerializableDateTimeOffset? protoDateTimeOffset)
    {
        if (protoDateTimeOffset == null)
        {
            return default;
        }

        return new DateTimeOffset(protoDateTimeOffset.Ticks, TimeSpan.FromMinutes(protoDateTimeOffset.OffsetMinutes));
    }

    /// <summary>
    /// Implicitly convert <see cref="DateTimeOffset"/> to <see cref="SerializableDateTimeOffset"/>.
    /// </summary>
    /// <param name="dateTimeOffset"><see cref="DateTimeOffset"/> to convert from.</param>
    /// <returns>A converted <see cref="SerializableDateTimeOffset"/>.</returns>
    public static implicit operator SerializableDateTimeOffset?(DateTimeOffset? dateTimeOffset)
    {
        if (dateTimeOffset == null)
        {
            return null;
        }

        return new SerializableDateTimeOffset
        {
            OffsetMinutes = dateTimeOffset.Value.Offset.TotalMinutes,
            Ticks = dateTimeOffset.Value.Ticks
        };
    }

    /// <inheritdoc/>
    public override string ToString() => ((DateTimeOffset?)this)?.ToString() ?? string.Empty;
}
