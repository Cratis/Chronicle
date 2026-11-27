// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { a_session, target } from './given/a_session';

describe('when the viewport and selection change', () => {
    let context: a_session;

    beforeEach(async () => {
        context = new a_session();
        context.session.attach();
        context.session.setVisible(target('a'), true);
        context.session.setVisible(target('b'), true);
        await vi.advanceTimersByTimeAsync(300);
    });
    afterEach(() => context.dispose());

    it('should read what scrolled into view once scrolling settles', () => [...context.reads].sort().should.deep.equal(['a', 'b']));

    it('should read a selection outside the viewport', async () => {
        context.session.select(target('elsewhere'));
        await vi.advanceTimersByTimeAsync(300);
        context.reads.should.include('elsewhere');
    });

    it('should not read again for the same selection reported twice', async () => {
        context.session.select(target('elsewhere'));
        context.session.select({ ...target('elsewhere') });
        await vi.advanceTimersByTimeAsync(300);
        context.reads.filter(id => id === 'elsewhere').length.should.equal(1);
    });

    it('should keep a scrolled-out observer from being read', async () => {
        context.session.setVisible(target('c'), true);
        context.session.setVisible(target('c'), false);
        await vi.advanceTimersByTimeAsync(300);
        context.reads.should.not.include('c');
    });
});
