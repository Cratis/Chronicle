// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ComparisonBudget } from './ComparisonBudget';
import { isRoundTripNumber } from './numbers';

export type Preflight = 'ok' | 'depth' | 'number';

const numberStart = /[-0-9]/;
const numberPart = /[-+0-9.eE]/;

const endOfString = (text: string, start: number): number => {
    let index = start + 1;
    while (index < text.length && text[index] !== '"') index += text[index] === '\\' ? 2 : 1;
    return index;
};

const endOfNumber = (text: string, start: number): number => {
    let end = start + 1;
    while (end < text.length && numberPart.test(text[end])) end++;
    return end;
};

const depthChange = (character: string): number => {
    if (character === '[' || character === '{') return 1;
    return character === ']' || character === '}' ? -1 : 0;
};

/**
 * Scans JSON text lexically, respecting string escapes, before anything recursive runs on it.
 * Reports nesting beyond the depth budget, and numeric tokens that cannot be compared safely.
 * @param text The JSON text, already checked against the size budget.
 * @returns `depth` when nesting is too deep, `number` when a numeric token is unsupported, otherwise `ok`.
 */
export const preflight = (text: string): Preflight => {
    let depth = 0;
    let unsupported = false;
    let index = 0;
    while (index < text.length) {
        const character = text[index];
        if (character === '"') {
            index = endOfString(text, index) + 1;
        } else if (numberStart.test(character)) {
            const end = endOfNumber(text, index);
            unsupported ||= !isRoundTripNumber(text.slice(index, end));
            index = end;
        } else {
            depth += depthChange(character);
            if (depth > ComparisonBudget.maxDepth) return 'depth';
            index++;
        }
    }
    return unsupported ? 'number' : 'ok';
};
