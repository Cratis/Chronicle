// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ContentComparison } from './ContentComparison';
import { ValueDiffer } from './ValueDiffer';
import { parseSide } from './parseSide';

/**
 * Compares two revision payloads (JSON text) read-only and within the comparison budgets.
 * Missing, malformed, oversized or numerically unsafe content is reported as such; it is never
 * replaced by another revision, and an incomplete walk is never reported as "no differences".
 * @param left The left payload text.
 * @param right The right payload text.
 * @returns The comparison result.
 */
export const compareContent = (left: string | null | undefined, right: string | null | undefined): ContentComparison => {
    const first = parseSide(left, 'left');
    if ('problem' in first) return first.problem;
    const second = parseSide(right, 'right');
    if ('problem' in second) return second.problem;
    const differ = new ValueDiffer();
    differ.compare(first.value, second.value);
    const outcome = differ.limit ? 'limited' : differ.rows.length > 0 ? 'different' : 'identical';
    return { outcome, rows: differ.rows, limit: differ.limit };
};
