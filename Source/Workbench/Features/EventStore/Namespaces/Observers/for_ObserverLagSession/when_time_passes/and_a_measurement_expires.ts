// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { a_session, target } from '../given/a_session';

describe('when time passes and a measurement expires between clock ticks', () => {
    let context: a_session;
    let changesBeforeExpiry: number;

    beforeEach(async () => {
        context = new a_session();
        context.validityMs = 2_000;
        context.session.select(target('a'));
        context.session.attach();
        await context.settle();
        changesBeforeExpiry = context.changes;
    });
    afterEach(() => context.dispose());

    it('should not move the clock before the expiry', async () => {
        await vi.advanceTimersByTimeAsync(1_500);
        context.changes.should.equal(changesBeforeExpiry);
    });

    it('should move the clock just past the expiry, well before the next clock tick', async () => {
        await vi.advanceTimersByTimeAsync(2_001);
        context.changes.should.be.greaterThan(changesBeforeExpiry);
        (context.session.now > Date.parse('2025-01-01T12:00:02Z')).should.be.true;
    });
});

describe('when time passes with several measurements', () => {
    it('should wake at the nearest expiry', async () => {
        const context = new a_session();
        try {
            context.validityMs = 20_000;
            context.session.select(target('a'));
            context.session.attach();
            await context.settle();
            const before = context.changes;
            await vi.advanceTimersByTimeAsync(20_001 - 15_000);
            // Only clock ticks so far; the expiry wake has not fired before 20s.
            const afterTicks = context.changes;
            await vi.advanceTimersByTimeAsync(15_000);
            (context.changes > afterTicks).should.be.true;
            (afterTicks >= before).should.be.true;
        } finally {
            context.dispose();
        }
    });
});
