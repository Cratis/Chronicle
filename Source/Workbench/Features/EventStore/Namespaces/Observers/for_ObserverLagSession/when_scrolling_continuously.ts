// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { a_session, target } from './given/a_session';

describe('when the viewport keeps changing faster than any wake is due', () => {
    let context: a_session;
    let readsBeforePoll: number;
    let changesBeforeExpiry: number;

    beforeEach(async () => {
        context = new a_session();
        context.validityMs = 40_000;
        context.session.setVisible(target('a'), true);
        context.session.attach();
        await vi.advanceTimersByTimeAsync(300);
        context.reads.length = 0;
        // Scroll every 100 ms: each update re-schedules, and must never push the armed wake later.
        for (let elapsed = 0; elapsed < 29_000; elapsed += 100) {
            context.session.setVisible(target(`row${elapsed}`), true);
            context.session.setVisible(target(`row${elapsed}`), false);
            await vi.advanceTimersByTimeAsync(100);
        }
        readsBeforePoll = context.reads.length;
        changesBeforeExpiry = context.changes;
        for (let elapsed = 0; elapsed < 14_000; elapsed += 100) {
            context.session.setVisible(target(`late${elapsed}`), true);
            context.session.setVisible(target(`late${elapsed}`), false);
            await vi.advanceTimersByTimeAsync(100);
        }
    });
    afterEach(() => context.dispose());

    it('should still fire the poll on time', () => (context.reads.length - readsBeforePoll).should.be.greaterThan(0));
    it('should still wake for the expiry on time', () => context.changes.should.be.greaterThan(changesBeforeExpiry));
});
