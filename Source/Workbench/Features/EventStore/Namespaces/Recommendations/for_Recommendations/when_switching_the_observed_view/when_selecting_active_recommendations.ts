// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_recommendations_page } from '../given/a_recommendations_page';

describe('when selecting active recommendations', given(a_recommendations_page, context => {
    beforeEach(async () => context.select());

    it('should enable the perform action', () => context.actions[0].disabled!.should.be.false);

    describe('when performing the selected recommendations', () => {
        beforeEach(async () => context.startPerform());

        it('should show the busy status', () => context.status.should.not.equal(''));
        it('should disable all actions', () => context.actions.every(action => action.disabled).should.be.true);

        describe('when the perform action completes', () => {
            beforeEach(async () => context.finishPerform());

            it('should clear the busy status', () => context.status.should.equal(''));
            it('should clear the selection', () => context.table.selectedItems!.should.have.lengthOf(0));
            it('should enable the toggle action', () => context.actions[3].disabled!.should.be.false);
        });
    });
}));
