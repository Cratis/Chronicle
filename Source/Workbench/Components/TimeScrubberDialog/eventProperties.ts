// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { EventPropertySummary } from './EventPropertySummary';

/**
 * The most properties a hover shows before it stops being readable at a glance.
 */
export const maximumProperties = 10;

/**
 * Renders one property value as the text a hover shows.
 *
 * An event carries whatever its author put on it, so this has to survive nested objects, arrays and
 * absent values rather than assume a primitive.
 * @param value The value to render.
 * @returns The value as text.
 */
export const formatPropertyValue = (value: unknown): string => {
    if (value === null || value === undefined) return '-';
    if (typeof value === 'string') return value;
    if (typeof value === 'number' || typeof value === 'boolean') return String(value);
    if (value instanceof Date) return value.toLocaleString();

    return JSON.stringify(value) ?? String(value);
};

/**
 * Summarizes an event's content into the properties a hover shows.
 *
 * The hover is a glance, not a viewer: past {@link maximumProperties} it says how many it left out
 * rather than growing into a wall of text that covers the read model behind it.
 * @param content The event's content.
 * @returns The {@link EventPropertySummary} to render.
 */
export const summarizeProperties = (content: Record<string, unknown> | null | undefined): EventPropertySummary => {
    const entries = Object.entries(content ?? {});

    return {
        properties: entries
            .slice(0, maximumProperties)
            .map(([name, value]) => ({ name, value: formatPropertyValue(value) })),
        remaining: Math.max(0, entries.length - maximumProperties)
    };
};
