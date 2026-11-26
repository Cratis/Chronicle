// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ComparisonBudget } from './ComparisonBudget';
import { ComparisonSide } from './ComparisonSide';
import { ContentComparison } from './ContentComparison';
import { preflight } from './preflight';

export type ParsedSide = { value: unknown } | { problem: ContentComparison };

const problem = (outcome: ContentComparison['outcome'], side: ComparisonSide, limit?: ContentComparison['limit']): ParsedSide =>
    ({ problem: { outcome, rows: [], side, limit } });

/**
 * Turns one side's JSON text into a value, or says why it cannot be compared. Never falls back to
 * the raw text and never includes parser messages, which can quote payload fragments.
 * @param text The JSON text; missing or blank means the content is unavailable.
 * @param side The side being parsed.
 * @returns The parsed value, or the comparison problem.
 */
export const parseSide = (text: string | null | undefined, side: ComparisonSide): ParsedSide => {
    if (typeof text !== 'string' || text.trim() === '') return problem('unavailable', side);
    if (text.length > ComparisonBudget.maxCharacters) return problem('limited', side, 'size');
    const scanned = preflight(text);
    if (scanned === 'depth') return problem('limited', side, 'depth');
    let value: unknown;
    try {
        value = JSON.parse(text);
    } catch {
        return problem('invalid', side);
    }
    return scanned === 'number' ? problem('unsupportedNumber', side) : { value };
};
