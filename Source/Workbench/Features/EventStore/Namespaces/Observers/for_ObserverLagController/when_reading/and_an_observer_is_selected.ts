// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_controller, target } from '../given/a_controller';

describe('when reading and an observer is selected', () => {
    let context: a_controller;

    beforeEach(() => {
        context = new a_controller({ maxOutstanding: 1 });
        context.show(target('a'), target('b'));
    });

    it('should read the selected observer ahead of the visible ones', () => {
        context.controller.select(target('b'));
        context.controller.refresh();
        context.requestedIds[0].should.equal('b');
    });

    it('should read it exactly once when it is also visible', async () => {
        context.controller.select(target('b'));
        context.controller.refresh();
        await context.complete(0);
        await context.complete(1);
        context.requestedIds.should.deep.equal(['b', 'a']);
    });

    it('should read it when it is outside the viewport', () => {
        context.controller.select(target('elsewhere'));
        context.requestedIds.should.deep.equal(['elsewhere']);
    });

    it('should read a newly selected observer before queued work', () => {
        context.controller.refresh();
        context.controller.select(target('later'));
        context.requestedIds.should.deep.equal(['a']);
    });

    it('should read the new selection next when the outstanding read completes', async () => {
        context.controller.refresh();
        context.controller.select(target('later'));
        await context.complete(0);
        context.requestedIds.should.deep.equal(['a', 'later']);
    });

    it('should abort the read of a selection that was left and is not visible', () => {
        context.controller.select(target('elsewhere'));
        context.controller.select(target('b'));
        context.reads[0].signal.aborted.should.be.true;
    });

    it('should not keep what an aborted read returns', async () => {
        context.controller.select(target('elsewhere'));
        context.controller.select(target('b'));
        await context.complete(0);
        context.controller.hasResult(target('elsewhere')).should.be.false;
    });
});
