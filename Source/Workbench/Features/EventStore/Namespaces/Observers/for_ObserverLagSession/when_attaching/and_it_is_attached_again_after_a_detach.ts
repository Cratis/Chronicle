// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { a_session, target } from '../given/a_session';

describe('when attaching again after a detach', () => {
    let context: a_session;

    beforeEach(async () => {
        context = new a_session();
        context.session.select(target('a'));
        context.session.attach();
        context.session.detach();
        context.session.attach();
        await context.settle();
    });
    afterEach(() => context.dispose());

    it('should still load the selected observer', () => (context.session.lagFor(target('a')) !== undefined).should.be.true);
    it('should report a completed read', () => context.session.hasResult(target('a')).should.be.true);
});

describe('when attaching again and the cells reported themselves before the first attach', () => {
    let context: a_session;

    beforeEach(async () => {
        context = new a_session();
        context.session.setVisible(target('a'), true);
        context.session.setVisible(target('b'), true);
        context.session.attach();
        context.session.detach();
        context.session.attach();
        await vi.advanceTimersByTimeAsync(300);
    });
    afterEach(() => context.dispose());

    it('should read the visible observers', () => [...new Set(context.reads)].sort().should.deep.equal(['a', 'b']));
});

describe('when detaching', () => {
    let context: a_session;

    beforeEach(async () => {
        context = new a_session();
        context.session.select(target('a'));
        context.session.attach();
        await context.settle();
        context.session.detach();
    });
    afterEach(() => context.dispose());

    it('should forget what was read', () => context.session.hasResult(target('a')).should.be.false);
});
