// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_recommendations_page } from '../given/a_recommendations_page';

describe('when toggling back to active recommendations', given(a_recommendations_page, context => {
    beforeEach(async () => {
        await context.toggle();
        await context.select();
        await context.toggle();
    });

    it('should register one active consumer', () => context.activeConsumer.subscriberCount.should.equal(1));
    it('should register one active listener', () => context.activeConsumer.listenerCount.should.equal(1));
    it('should release the ignored consumer', () => context.ignoredConsumer.subscriberCount.should.equal(0));
    it('should remove the ignored listener', () => context.ignoredConsumer.listenerCount.should.equal(0));
    it('should reuse the retained active transport', () => context.activeSubscriptions.should.have.lengthOf(1));
    it('should show the active query', () => context.table.query.should.equal(context.activeQuery));
    it('should clear the ignored selection', () => context.table.selectedItems!.should.have.lengthOf(0));
}));
