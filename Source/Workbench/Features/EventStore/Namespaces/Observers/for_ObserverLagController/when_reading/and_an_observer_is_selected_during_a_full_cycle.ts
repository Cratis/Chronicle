// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_controller, target } from '../given/a_controller';

describe('when an observer is selected during a full cycle', () => {
    let context: a_controller;

    beforeEach(async () => {
        context = new a_controller();
        context.show(...Array.from({ length: 50 }, (_, index) => target(`o${index}`)));
        context.controller.refresh();
        context.controller.select(target('elsewhere'));
        for (let index = 0; index < context.reads.length; index++) await context.complete(index);
    });

    it('should read the selection, not make it wait for the next cycle', () => context.requestedIds.should.include('elsewhere'));
    it('should read it ahead of the queued rows', () => context.requestedIds.indexOf('elsewhere').should.be.at.most(4));
    it('should still stay within the cycle budget', () => context.reads.length.should.equal(20));
    it('should never have more than four reads outstanding at the start', () => context.reads.slice(0, 4).length.should.equal(4));
});
