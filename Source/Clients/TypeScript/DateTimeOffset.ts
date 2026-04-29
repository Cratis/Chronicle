// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// SerializableDateTimeOffset is duplicated across all generated proto files that reference it.
// The canonical source is eventsequences, matching the generated barrel export order.
import type { SerializableDateTimeOffset } from './generated/eventsequences';

/**
 * Converts a {@link SerializableDateTimeOffset} from its ISO 8601 wire format to a JavaScript {@link Date}.
 * @param value The {@link SerializableDateTimeOffset} from the gRPC message.
 * @returns A {@link Date}, or undefined if the value is not set.
 */
export function toDate(value: SerializableDateTimeOffset | undefined): Date | undefined {
    if (!value?.Value) return undefined;
    return new Date(value.Value);
}

/**
 * Converts a JavaScript {@link Date} to a {@link SerializableDateTimeOffset} for use in gRPC messages.
 * The Date is serialized as an ISO 8601 string (e.g., "2024-01-15T12:30:00.000Z").
 * @param date The {@link Date} to convert.
 * @returns A {@link SerializableDateTimeOffset} with the ISO 8601 value, or undefined if the date is not set.
 */
export function fromDate(date: Date | undefined): SerializableDateTimeOffset | undefined {
    if (!date) return undefined;
    return { Value: date.toISOString() };
}
