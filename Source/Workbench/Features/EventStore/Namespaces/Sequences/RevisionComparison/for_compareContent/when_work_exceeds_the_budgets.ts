// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ComparisonBudget } from '../ComparisonBudget';
import { compareContent } from '../compareContent';

const numbers = (count: number, offset: number) => JSON.stringify(Array.from({ length: count }, (_, index) => index + offset));
const members = (count: number) => JSON.stringify(Object.fromEntries(Array.from({ length: count }, (_, index) => [`k${index}`, index])));

describe('when exactly the maximum number of rows differ', () => {
    const result = compareContent(numbers(ComparisonBudget.maxRows, 0), numbers(ComparisonBudget.maxRows, 1));
    it('should list all rows', () => result.rows.length.should.equal(ComparisonBudget.maxRows));
    it('should be complete', () => result.outcome.should.equal('different'));
});

describe('when one more row than the maximum differs', () => {
    const result = compareContent(numbers(ComparisonBudget.maxRows + 1, 0), numbers(ComparisonBudget.maxRows + 1, 1));
    it('should stop at the maximum', () => result.rows.length.should.equal(ComparisonBudget.maxRows));
    it('should be limited by rows', () => `${result.outcome} ${result.limit}`.should.equal('limited rows'));
});

describe('when wide identical content exceeds the work budget', () => {
    const wide = members(ComparisonBudget.maxVisited);
    const result = compareContent(wide, wide);
    it('should not claim there are no differences', () => result.outcome.should.equal('limited'));
    it('should be limited by work', () => result.limit!.should.equal('nodes'));
});

describe('when a long identical array exceeds the work budget', () => {
    const result = compareContent(numbers(ComparisonBudget.maxVisited, 0), numbers(ComparisonBudget.maxVisited, 0));
    it('should not claim there are no differences', () => result.outcome.should.equal('limited'));
});

describe('when a long path is reported', () => {
    const key = 'k'.repeat(ComparisonBudget.maxPath + 10);
    const result = compareContent(`{"${key}":1}`, `{"${key}":2}`);
    it('should bound the path', () => result.rows[0].path.length.should.equal(ComparisonBudget.maxPath + 1));
    it('should be limited by path', () => result.limit!.should.equal('path'));
});

describe('when a long scalar is reported', () => {
    const result = compareContent(`{"v":"${'a'.repeat(1000)}1"}`, `{"v":"${'a'.repeat(1000)}2"}`);
    it('should bound the preview', () => result.rows[0].before!.length.should.be.at.most(ComparisonBudget.maxPreview + 3));
    it('should be limited by preview', () => result.limit!.should.equal('preview'));
});
