// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ComparisonBudget } from './ComparisonBudget';
import { hasOwn } from './hasOwn';

export interface ValueDescription {
    text: string;
    truncated: boolean;
}

const bounded = (text: string): ValueDescription =>
    text.length > ComparisonBudget.maxPreview
        ? { text: `${text.slice(0, ComparisonBudget.maxPreview)}…`, truncated: true }
        : { text, truncated: false };

/**
 * Describes a parsed JSON value for display without serializing nested content: scalars are
 * shown (bounded), containers as their kind and size only.
 * @param value The parsed value.
 * @returns The bounded text and whether it was cut short.
 */
export const describeValue = (value: unknown): ValueDescription => {
    if (Array.isArray(value)) return { text: `array(${value.length})`, truncated: false };
    if (value !== null && typeof value === 'object') {
        let count = 0;
        for (const key in value) if (hasOwn(value, key)) count++;
        return { text: `object(${count})`, truncated: false };
    }
    if (typeof value === 'string') {
        const shown = bounded(value);
        return { text: JSON.stringify(shown.text.slice(0, ComparisonBudget.maxPreview)) + (shown.truncated ? '…' : ''), truncated: shown.truncated };
    }
    return bounded(String(value));
};
