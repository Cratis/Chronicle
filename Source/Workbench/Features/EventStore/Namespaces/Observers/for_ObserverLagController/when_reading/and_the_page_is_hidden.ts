// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { a_controller, target } from '../given/a_controller';

describe('when reading and the page is hidden', () => {
    let context: a_controller;

    beforeEach(() => {
        context = new a_controller();
        context.show(target('a'));
        context.controller.setPaused(true);
    });

    it('should not start a read', () => {
        context.controller.refresh();
        context.reads.length.should.equal(0);
    });

    it('should abort what was outstanding when it is hidden', () => {
        const other = new a_controller();
        other.show(target('a'));
        other.controller.refresh();
        other.controller.setPaused(true);
        other.reads[0].signal.aborted.should.be.true;
    });

    it('should read again when it is shown', () => {
        context.controller.setPaused(false);
        context.reads.length.should.equal(1);
    });
});
