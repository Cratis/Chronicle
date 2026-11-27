// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_controller, target } from '../given/a_controller';

describe('when reading and the read does not succeed', () => {
    let context: a_controller;

    beforeEach(async () => {
        context = new a_controller();
        context.show(target('a'));
        context.controller.refresh();
    });

    it('should record an unavailable result when the read rejects', async () => {
        context.reads[0].reject(new Error('boom'));
        await context.settle();
        context.controller.hasResult(target('a')).should.be.true;
        (context.controller.get(target('a')) === undefined).should.be.true;
    });

    it('should not keep an earlier healthy measurement when a later read fails', async () => {
        await context.complete(0);
        context.controller.refresh();
        context.reads[1].resolve(undefined);
        await context.settle();
        (context.controller.get(target('a')) === undefined).should.be.true;
    });

    it('should release its slot when the read rejects', async () => {
        context.reads[0].reject(new Error('boom'));
        await context.settle();
        context.controller.isBusy.should.be.false;
    });
});
