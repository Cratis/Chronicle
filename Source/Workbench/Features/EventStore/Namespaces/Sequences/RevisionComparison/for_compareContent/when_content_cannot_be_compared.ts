// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ComparisonBudget } from '../ComparisonBudget';
import { compareContent } from '../compareContent';

const nested = (depth: number) => '['.repeat(depth) + ']'.repeat(depth);

const scenarios: [string, string | undefined | null, string | undefined, string, string | undefined, string | undefined][] = [
    ['left is missing', undefined, '{}', 'unavailable', 'left', undefined],
    ['left is null instead of text', null, '{}', 'unavailable', 'left', undefined],
    ['right is blank', '{}', '  ', 'unavailable', 'right', undefined],
    ['left is malformed', '{"a":', '{}', 'invalid', 'left', undefined],
    ['right is a bare word', '{}', 'hello', 'invalid', 'right', undefined],
    ['left is oversized', 'x'.repeat(ComparisonBudget.maxCharacters + 1), '{}', 'limited', 'left', 'size'],
    ['depth is one over', nested(ComparisonBudget.maxDepth + 1), '{}', 'limited', 'left', 'depth'],
    ['adjacent integers beyond 2^53', '{"n":9007199254740993}', '{"n":9007199254740992}', 'unsupportedNumber', 'left', undefined],
    ['a number overflows', '{"n":1e999}', '{"n":1e999}', 'unsupportedNumber', 'left', undefined],
    ['a decimal loses precision', '{"n":0.1234567890123456789}', '{"n":0.1234567890123456789}', 'unsupportedNumber', 'left', undefined],
    ['a number underflows to zero', '{"n":1e-999}', '{"n":0}', 'unsupportedNumber', 'left', undefined]
];

for (const [name, left, right, outcome, side, limit] of scenarios) {
    describe(`when ${name}`, () => {
        const result = compareContent(left, right);
        it('should state the outcome', () => result.outcome.should.equal(outcome));
        it('should name the side', () => result.side!.should.equal(side));
        it('should name the budget', () => (result.limit ?? 'none').should.equal(limit ?? 'none'));
        it('should show no rows', () => result.rows.should.be.empty);
    });
}

describe('when content is exactly at the size budget', () => {
    const text = `"${'x'.repeat(ComparisonBudget.maxCharacters - 2)}"`;
    const result = compareContent(text, text);
    it('should compare completely', () => result.outcome.should.equal('identical'));
});

describe('when depth is exactly at the budget', () => {
    const result = compareContent(nested(ComparisonBudget.maxDepth), nested(ComparisonBudget.maxDepth));
    it('should compare completely', () => result.outcome.should.equal('identical'));
});

describe('when brackets appear inside strings', () => {
    const text = JSON.stringify({ v: '['.repeat(100) + '\\"' + '1e999' });
    it('should not count them as nesting or numbers', () => compareContent(text, text).outcome.should.equal('identical'));
});
