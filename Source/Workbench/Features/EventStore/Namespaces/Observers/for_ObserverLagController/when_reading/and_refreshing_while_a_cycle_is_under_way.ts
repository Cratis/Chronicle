// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_controller, target } from '../given/a_controller';

describe('when refreshing while a cycle is under way', () => {
    let context: a_controller;

    beforeEach(async () => {
        context = new a_controller();
        context.show(...Array.from({ length: 50 }, (_, index) => target(`o${index}`)));
        context.controller.refresh();
        context.controller.refresh();
        for (let index = 0; index < context.reads.length; index++) await context.complete(index);
    });

    it('should not read more than the cycle budget once drained', () => context.reads.length.should.equal(20));
    it('should not leave anything queued', () => context.controller.isBusy.should.be.false);
});

describe('when the cache is full', () => {
    it('should never hold more than its cap, visible entries included', async () => {
        const context = new a_controller({ maxEntries: 3 });
        context.show(...Array.from({ length: 8 }, (_, index) => target(`o${index}`)));
        context.controller.refresh();
        for (let index = 0; index < 8; index++) await context.complete(index);
        const held = Array.from({ length: 8 }, (_, index) => context.controller.hasResult(target(`o${index}`))).filter(Boolean).length;
        held.should.be.at.most(3);
    });
});
