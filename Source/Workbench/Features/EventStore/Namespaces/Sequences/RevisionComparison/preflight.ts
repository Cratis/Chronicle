// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ComparisonBudget } from './ComparisonBudget';
import { isRoundTripNumber } from './numbers';

export type Preflight = 'ok' | 'depth' | 'number';

const numberStart = /[-0-9]/;
const numberPart = /[-+0-9.eE]/;

/**
 * Scans JSON text lexically, respecting string escapes, before anything recursive runs on it.
 * Reports nesting beyond the depth budget, and numeric tokens that cannot be compared safely.
 * @param text The JSON text, already checked against the size budget.
 * @returns `depth` when nesting is too deep, `number` when a numeric token is unsupported, otherwise `ok`.
 */
export const preflight = (text: string): Preflight => {
    let depth = 0;
    let unsupported = false;
    let inString = false;
    for (let index = 0; index < text.length; index++) {
        const character = text[index];
        if (inString) {
            if (character === '\\') index++;
            else if (character === '"') inString = false;
        } else if (character === '"') {
            inString = true;
        } else if (character === '[' || character === '{') {
            if (++depth > ComparisonBudget.maxDepth) return 'depth';
        } else if (character === ']' || character === '}') {
            depth--;
        } else if (numberStart.test(character)) {
            let end = index + 1;
            while (end < text.length && numberPart.test(text[end])) end++;
            if (!isRoundTripNumber(text.slice(index, end))) unsupported = true;
            index = end - 1;
        }
    }
    return unsupported ? 'number' : 'ok';
};
