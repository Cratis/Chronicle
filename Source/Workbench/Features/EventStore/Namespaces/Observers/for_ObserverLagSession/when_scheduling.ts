// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { boundedDelay, maxTimerDelayMs } from '../ObserverLagSession';
import { a_session, target } from './given/a_session';

describe('when attaching while the page is hidden', () => {
    let context: a_session;

    beforeEach(async () => {
        context = new a_session();
        context.hidden = true;
        context.session.select(target('a'));
        context.session.attach();
        await vi.advanceTimersByTimeAsync(120_000);
    });
    afterEach(() => context.dispose());

    it('should not read the preselected observer', () => context.reads.should.deep.equal([]));
});

describe('when the page is shown just before the poll is due', () => {
    let context: a_session;

    beforeEach(async () => {
        context = new a_session();
        context.session.setVisible(target('a'), true);
        context.session.attach();
        await vi.advanceTimersByTimeAsync(29_900);
        context.setHidden(true);
        context.reads.length = 0;
        context.setHidden(false);
        await vi.advanceTimersByTimeAsync(2_000);
    });
    afterEach(() => context.dispose());

    it('should read once, not once for the resume and again for the poll', () => context.reads.length.should.equal(1));
});

describe('when a measurement expires at the very moment the clock was captured', () => {
    let context: a_session;
    let changesBefore: number;
    let nowBefore: number;

    beforeEach(async () => {
        context = new a_session();
        context.validityMs = 0;
        context.session.select(target('a'));
        context.session.attach();
        await context.settle();
        changesBefore = context.changes;
        nowBefore = context.session.now;
        await vi.advanceTimersByTimeAsync(5);
    });
    afterEach(() => context.dispose());

    it('should still wake and re-judge, not skip the wake', () => context.changes.should.be.greaterThan(changesBefore));
    it('should move the clock past the expiry', () => context.session.now.should.be.greaterThan(nowBefore));
});

describe('when the nearest wake is further away than a timer can wait', () => {
    it('should wait no longer than a timer can', () => boundedDelay([2 ** 40], 0).should.equal(maxTimerDelayMs));
    it('should not wait a negative time for something already due', () => boundedDelay([5], 10).should.equal(0));
    it('should wait for the earliest of what is due', () => boundedDelay([900, 400, 700], 100).should.equal(300));
});
