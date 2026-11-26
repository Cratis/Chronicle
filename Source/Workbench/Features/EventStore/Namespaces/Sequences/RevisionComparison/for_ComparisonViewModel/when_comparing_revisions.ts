// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ComparisonViewModel } from '../ComparisonViewModel';
import { eventWith, revision, scope } from '../given/events';

const opened = (revisions = [revision('{"quantity":2,"status":"new"}', 2, 5), revision('{"quantity":3,"status":"new"}', 3, 10)]) => {
    const viewModel = new ComparisonViewModel();
    viewModel.handleProps({ scope, event: eventWith(revisions) });
    viewModel.open();
    return viewModel;
};

describe('when opening the comparison', () => {
    const viewModel = opened();
    it('should compare the original with the latest', () => `${viewModel.left!.ordinal}-${viewModel.right!.ordinal}`.should.equal('0-2'));
    it('should report the differences', () => viewModel.comparison!.rows.map(row => row.path).should.deep.equal(['/quantity']));
});

describe('when comparing a revision with itself', () => {
    const viewModel = opened();
    viewModel.selectLeft(viewModel.rightKey!);
    it('should report no differences', () => viewModel.comparison!.outcome.should.equal('identical'));
});

describe('when comparing adjacent revisions', () => {
    const viewModel = opened();
    viewModel.selectLeft(viewModel.revisions.choices[1].key);
    it('should compare exactly those', () => viewModel.comparison!.rows.map(row => `${row.before}>${row.after}`).should.deep.equal(['2>3']));
});

describe('when a new revision arrives while comparing', () => {
    const viewModel = opened();
    const [left, right] = [viewModel.leftKey, viewModel.rightKey];
    viewModel.handleProps({ scope, event: eventWith([revision('{"quantity":2,"status":"new"}', 2, 5), revision('{"quantity":3,"status":"new"}', 3, 10), revision('{"quantity":4}', 4, 15)]) });
    it('should keep the left side', () => viewModel.leftKey!.should.equal(left!));
    it('should keep the right side on the same revision', () => viewModel.rightKey!.should.equal(right!));
    it('should keep showing the same revision', () => viewModel.right!.ordinal.should.equal(2));
});

describe('when the selected revision disappears', () => {
    const viewModel = opened();
    viewModel.handleProps({ scope, event: eventWith([revision('{"quantity":2,"status":"new"}', 2, 5)]) });
    it('should report the side unavailable', () => `${viewModel.comparison!.outcome} ${viewModel.comparison!.side}`.should.equal('unavailable right'));
    it('should not substitute another revision', () => (viewModel.right === undefined).should.be.true);
});

describe('when the selected revision becomes ambiguous', () => {
    const viewModel = opened();
    viewModel.handleProps({ scope, event: eventWith([revision('{"a":1}', 2, 5), revision('{"quantity":3}', 3, 10), revision('{"quantity":9}', 3, 10)]) });
    it('should report that side unavailable', () => viewModel.comparison!.outcome.should.equal('unavailable'));
});

describe('when the event changes', () => {
    const viewModel = opened();
    viewModel.handleProps({ scope, event: eventWith([revision('{}', 2, 5)], 'order-placed', '43') });
    it('should close and clear the selection', () => `${viewModel.isOpen} ${viewModel.leftKey}`.should.equal('false undefined'));
    it('should offer no comparison', () => (viewModel.comparison === undefined).should.be.true);
});

describe('when the scope changes', () => {
    const viewModel = opened();
    viewModel.handleProps({ scope: { ...scope, eventSequenceId: 'other' }, event: eventWith([revision('{}', 2, 5)]) });
    it('should reset', () => viewModel.isOpen.should.be.false);
});

describe('when the event is redacted while comparing', () => {
    const viewModel = opened();
    viewModel.handleProps({ scope, event: eventWith([revision('{"secret":1}', 2, 5)], 'EventRedacted') });
    it('should discard the comparison rows', () => (viewModel.comparison === undefined).should.be.true);
    it('should clear the selection', () => (viewModel.leftKey === undefined && viewModel.rightKey === undefined).should.be.true);
    it('should report redaction', () => viewModel.revisions.state.should.equal('redacted'));
});

describe('when the comparison is closed', () => {
    const viewModel = opened();
    viewModel.close();
    it('should hold no selection', () => (viewModel.leftKey === undefined && viewModel.rightKey === undefined).should.be.true);
    it('should hold no comparison', () => (viewModel.comparison === undefined).should.be.true);
    it('should choose fresh defaults when reopened', () => {
        viewModel.open();
        viewModel.right!.ordinal.should.equal(2);
    });
});

describe('when history appears while the comparison is open', () => {
    const viewModel = new ComparisonViewModel();
    viewModel.handleProps({ scope, event: eventWith([]) });
    viewModel.open();
    viewModel.handleProps({ scope, event: eventWith([revision('{"quantity":2}', 2, 5)]) });
    it('should resolve defaults', () => viewModel.comparison!.outcome.should.equal('different'));
});

describe('when the original has no content', () => {
    const event = eventWith([revision('{"a":1}', 2, 5), revision('{"a":2}', 3, 10)]);
    event.originalContent = '';
    const viewModel = new ComparisonViewModel();
    viewModel.handleProps({ scope, event });
    viewModel.open();
    it('should default to the first revision with content', () => viewModel.left!.ordinal.should.equal(1));
    it('should still report that original unavailable when chosen', () => {
        viewModel.selectLeft('original');
        viewModel.comparison!.outcome.should.equal('unavailable');
    });
});
