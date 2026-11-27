// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { a_session, target } from '../given/a_session';

describe('when the page is shown again after being hidden', () => {
    let context: a_session;
    let changesWhileHidden: number;
    let nowWhileHidden: number;

    beforeEach(async () => {
        context = new a_session();
        context.session.select(target('a'));
        context.session.attach();
        await context.settle();
        context.setHidden(true);
        nowWhileHidden = context.session.now;
        changesWhileHidden = context.changes;
        vi.setSystemTime(Date.now() + 10 * 60_000);
        context.setHidden(false);
    });
    afterEach(() => context.dispose());

    it('should re-judge the measurements at once', () => (context.session.now - nowWhileHidden).should.be.at.least(10 * 60_000));
    it('should tell the view to render again', () => context.changes.should.be.greaterThan(changesWhileHidden));
});

describe('when the page is shown again with nothing left to read', () => {
    let context: a_session;
    let nowWhileHidden: number;

    beforeEach(() => {
        context = new a_session();
        context.session.attach();
        context.setHidden(true);
        nowWhileHidden = context.session.now;
        vi.setSystemTime(Date.now() + 10 * 60_000);
        context.setHidden(false);
    });
    afterEach(() => context.dispose());

    it('should re-judge at once, without waiting for a measurement or a clock tick', () =>
        (context.session.now - nowWhileHidden).should.be.at.least(10 * 60_000));
});

describe('when the page is hidden', () => {
    let context: a_session;

    beforeEach(async () => {
        context = new a_session();
        context.hidden = true;
        context.session.setVisible(target('a'), true);
        context.session.attach();
        await vi.advanceTimersByTimeAsync(120_000);
    });
    afterEach(() => context.dispose());

    it('should not read', () => context.reads.should.deep.equal([]));
});
