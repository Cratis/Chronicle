// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_controller, target } from '../given/a_controller';

const ids = (count: number) => Array.from({ length: count }, (_, index) => `o${index}`);

describe('when reading and many observers are visible', () => {
    let context: a_controller;

    beforeEach(() => {
        context = new a_controller();
        context.show(...ids(50).map(id => target(id)));
        context.controller.refresh();
    });

    it('should read at most the cycle limit of them', async () => {
        for (let index = 0; index < 20; index++) await context.complete(index);
        context.reads.length.should.equal(20);
    });

    it('should never have more than four reads outstanding', () => context.reads.length.should.equal(4));

    it('should start the next read when one completes', async () => {
        await context.complete(0);
        context.reads.length.should.equal(5);
    });

    it('should not start a second cycle while the first is under way', () => {
        context.controller.refresh();
        context.reads.length.should.equal(4);
    });

    it('should read the ones it left out in the next cycle', async () => {
        for (let index = 0; index < 20; index++) await context.complete(index);
        context.controller.refresh();
        context.requestedIds.slice(20, 24).should.deep.equal(['o20', 'o21', 'o22', 'o23']);
    });
});
