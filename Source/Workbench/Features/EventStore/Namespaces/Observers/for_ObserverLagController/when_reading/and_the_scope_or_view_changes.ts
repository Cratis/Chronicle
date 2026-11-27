// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_controller, measurement, target } from '../given/a_controller';

describe('when reading and the scope or view changes', () => {
    let context: a_controller;

    beforeEach(() => {
        context = new a_controller();
        context.show(target('a'));
        context.controller.refresh();
    });

    it('should keep the same observer id in different scopes apart', async () => {
        context.show(target('a', 'other'));
        context.controller.refresh();
        await context.complete(0, measurement('first'));
        await context.complete(1, measurement('second'));
        context.controller.get(target('a'))!.should.not.equal(context.controller.get(target('a', 'other')));
    });

    it('should keep the same observer id in different event sequences apart', async () => {
        await context.complete(0, measurement('log'));
        (context.controller.get(target('a', 'ns', 'system')) === undefined).should.be.true;
    });

    it('should discard a completion after the controller is disposed', async () => {
        context.controller.dispose();
        await context.complete(0);
        context.changes.should.equal(0);
        context.controller.hasResult(target('a')).should.be.false;
    });

    it('should abort what is outstanding when it is disposed', () => {
        context.controller.dispose();
        context.reads[0].signal.aborted.should.be.true;
    });

    it('should discard a completion for an observer that scrolled out of view', async () => {
        context.controller.setVisible(target('a'), false);
        await context.complete(0);
        context.controller.hasResult(target('a')).should.be.false;
    });

    it('should drop queued reads for observers that scrolled out of view', async () => {
        const slow = new a_controller({ maxOutstanding: 1 });
        slow.show(target('a'), target('b'));
        slow.controller.refresh();
        slow.controller.setVisible(target('b'), false);
        await slow.complete(0);
        slow.requestedIds.should.deep.equal(['a']);
    });

    it('should not read again when visibility is reported repeatedly', () => {
        context.show(target('a'));
        context.show(target('a'));
        context.controller.refresh();
        context.requestedIds.should.deep.equal(['a']);
    });

    it('should forget observers that are not on screen first when the cache is full', async () => {
        const small = new a_controller({ maxEntries: 2 });
        small.show(target('a'), target('b'));
        small.controller.refresh();
        await small.complete(0);
        await small.complete(1);
        small.controller.setVisible(target('a'), false);
        small.show(target('c'));
        small.controller.refresh();
        await small.complete(3);
        [small.controller.hasResult(target('a')), small.controller.hasResult(target('b')), small.controller.hasResult(target('c'))]
            .should.deep.equal([false, true, true]);
    });
});
