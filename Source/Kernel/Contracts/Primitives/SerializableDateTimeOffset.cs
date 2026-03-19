// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Contracts.Primitives;

/// <summary>
/// Proto contract that represents a <see cref="DateTimeOffset"/> as an ISO 8601 string.
/// </summary>
/// <remarks>
/// The value is serialized using the round-trip format specifier "O",
/// producing strings in the form yyyy-MM-ddTHH:mm:ss.fffffffzzz
/// (e.g., "2024-01-15T12:30:00.0000000+02:00").
/// </remarks>
[ProtoContract]
public class SerializableDateTimeOffset
{
    /// <summary>
    /// Gets or sets the ISO 8601 string representation of the <see cref="DateTimeOffset"/>.
    /// Format: yyyy-MM-ddTHH:mm:ss.fffffffzzz (e.g., "2024-01-15T12:30:00.0000000+02:00").
    /// </summary>
    [ProtoMember(1)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Implicitly converts a <see cref="SerializableDateTimeOffset"/> to a nullable <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="protoDateTimeOffset"><see cref="SerializableDateTimeOffset"/> to convert from.</param>
    /// <returns>A converted <see cref="DateTimeOffset"/>, or null if the input is null or empty.</returns>
    public static implicit operator DateTimeOffset?(SerializableDateTimeOffset? protoDateTimeOffset)
    {
        if (protoDateTimeOffset is null || string.IsNullOrEmpty(protoDateTimeOffset.Value))
        {
            return null;
        }

        return DateTimeOffset.Parse(protoDateTimeOffset.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    /// <summary>
    /// Implicitly converts a <see cref="SerializableDateTimeOffset"/> to a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="protoDateTimeOffset"><see cref="SerializableDateTimeOffset"/> to convert from.</param>
    /// <returns>A converted <see cref="DateTimeOffset"/>, or <see cref="DateTimeOffset.MinValue"/> if the input is null or empty.</returns>
    public static implicit operator DateTimeOffset(SerializableDateTimeOffset? protoDateTimeOffset)
    {
        if (protoDateTimeOffset is null || string.IsNullOrEmpty(protoDateTimeOffset.Value))
        {
            return default;
        }

        return DateTimeOffset.Parse(protoDateTimeOffset.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    /// <summary>
    /// Implicitly converts a nullable <see cref="DateTimeOffset"/> to a <see cref="SerializableDateTimeOffset"/>.
    /// </summary>
    /// <param name="dateTimeOffset"><see cref="DateTimeOffset"/> to convert from.</param>
    /// <returns>A <see cref="SerializableDateTimeOffset"/> with an ISO 8601 value, or null if the input is null.</returns>
    public static implicit operator SerializableDateTimeOffset?(DateTimeOffset? dateTimeOffset)
    {
        if (dateTimeOffset is null)
        {
            return null;
        }

        return new SerializableDateTimeOffset { Value = dateTimeOffset.Value.ToString("O", CultureInfo.InvariantCulture) };
    }

    /// <summary>
    /// Implicitly converts a <see cref="DateTimeOffset"/> to a <see cref="SerializableDateTimeOffset"/>.
    /// </summary>
    /// <param name="dateTimeOffset"><see cref="DateTimeOffset"/> to convert from.</param>
    /// <returns>A <see cref="SerializableDateTimeOffset"/> with an ISO 8601 value.</returns>
    public static implicit operator SerializableDateTimeOffset(DateTimeOffset dateTimeOffset) =>
        new() { Value = dateTimeOffset.ToString("O", CultureInfo.InvariantCulture) };

    /// <inheritdoc/>
    public override string ToString() => Value ?? string.Empty;
}
