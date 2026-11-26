// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ComparisonBudget } from '../ComparisonBudget';
import { resolveRevisionChoices } from '../revisionChoices';
import { eventWith, revision } from '../given/events';

describe('when the event has retained revisions', () => {
    const result = resolveRevisionChoices(eventWith([revision('{"quantity":2}', 2, 5), revision('{"quantity":3}', 3, 10)]));
    it('should be ready', () => result.state.should.equal('ready'));
    it('should offer the original then each revision', () => result.choices.map(choice => choice.ordinal).should.deep.equal([0, 1, 2]));
    it('should give the original generation 1', () => result.choices[0].generation.should.equal(1));
    it('should give each revision its own generation', () => result.choices.slice(1).map(choice => choice.generation).should.deep.equal([2, 3]));
    it('should use the original content for the original', () => result.choices[0].content!.should.equal('{"quantity":1,"status":"new"}'));
    it('should use ordinal-independent identities', () => new Set(result.choices.map(choice => choice.key)).size.should.equal(3));
});

describe('when a revision is inserted before another', () => {
    const second = revision('{"quantity":3}', 3, 10);
    const before = resolveRevisionChoices(eventWith([second]));
    const after = resolveRevisionChoices(eventWith([revision('{"quantity":2}', 2, 5), second]));
    it('should keep the identity of the existing revision', () => after.choices[2].key.should.equal(before.choices[1].key));
    it('should change its display ordinal only', () => `${before.choices[1].ordinal}->${after.choices[2].ordinal}`.should.equal('1->2'));
});

for (const [name, revisions] of [
    ['none', []],
    ['an invalid timestamp', [{ ...revision('{}', 2, 1), occurred: new Date(NaN) }]],
    ['a missing correlation id', [revision('{}', 2, 1, '')]],
    ['an over-long correlation id', [revision('{}', 2, 1, 'c'.repeat(ComparisonBudget.maxIdentity + 1))]],
    ['a duplicated identity', [revision('{"a":1}', 2, 1, 'same'), revision('{"a":2}', 2, 1, 'same')]]
] as const) {
    describe(`when the revisions are ${name}`, () => {
        const result = resolveRevisionChoices(eventWith([...revisions] as never));
        it('should not offer an unidentifiable revision', () => result.choices.filter(choice => choice.ordinal > 0).should.be.empty);
        it('should account for the omitted revisions', () => (result.state === 'noHistory' ? 0 : result.ambiguous).should.equal(revisions.length));
    });
}

describe('when the event is redacted but a hostile response still carries history', () => {
    const result = resolveRevisionChoices(eventWith([revision('{"secret":1}', 2, 5)], 'EventRedacted'));
    it('should be redacted', () => result.state.should.equal('redacted'));
    it('should offer nothing', () => result.choices.should.be.empty);
});

describe('when erased paths are confirmed', () => {
    const event = eventWith([revision('{}', 2, 5)]);
    event.erasedProperties = ['/name'];
    const result = resolveRevisionChoices(event);
    it('should refuse comparison', () => result.state.should.equal('erased'));
});

describe('when only a revised generation is known', () => {
    const event = eventWith([]);
    event.revisedGeneration = 2;
    it('should report no retained history rather than unchanged', () => resolveRevisionChoices(event).state.should.equal('noHistory'));
});

describe('when there are too many retained revisions', () => {
    const revisions = Array.from({ length: ComparisonBudget.maxRevisions + 1 }, (_, index) => revision('{}', 2, index % 50, `c${index}`));
    const result = resolveRevisionChoices(eventWith(revisions));
    it('should be limited rather than truncated', () => result.state.should.equal('limited'));
    it('should offer no choices', () => result.choices.should.be.empty);
});

describe('when the revisions are at the maximum', () => {
    const revisions = Array.from({ length: ComparisonBudget.maxRevisions }, (_, index) => revision('{}', 2, index % 50, `c${index}`));
    it('should be ready', () => resolveRevisionChoices(eventWith(revisions)).state.should.equal('ready'));
});

describe('when there is no event', () => {
    it('should have no history', () => resolveRevisionChoices(undefined).state.should.equal('noHistory'));
});
