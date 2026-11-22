// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_recommendations_page } from '../given/a_recommendations_page';

describe('when selecting ignored recommendations', given(a_recommendations_page, context => {
    beforeEach(async () => {
        await context.toggle();
        await context.select();
    });

    it('should enable the restore action', () => context.actions[2].disabled!.should.be.false);
    it('should disable the perform action', () => context.actions[0].disabled!.should.be.true);

    describe('when restoring the selected recommendations', () => {
        beforeEach(async () => context.restore());

        it('should execute the restore command once', () => context.restoreCalls.should.have.lengthOf(1));
        it('should clear the selection', () => context.table.selectedItems!.should.have.lengthOf(0));
        it('should disable the restore action', () => context.actions[2].disabled!.should.be.true);
        it('should clear the busy status', () => context.status.should.equal(''));
    });
}));
