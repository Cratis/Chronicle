// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_recommendations_page } from '../given/a_recommendations_page';

describe('when toggling to ignored recommendations', given(a_recommendations_page, context => {
    beforeEach(async () => {
        await context.select();
        await context.toggle();
    });

    it('should release the active consumer', () => context.activeConsumer.subscriberCount.should.equal(0));
    it('should remove the active listener', () => context.activeConsumer.listenerCount.should.equal(0));
    // Cache retention may keep the transport alive, but not a mounted consumer.
    it('should allow the active transport to remain retained', () => context.activeConsumer.subscribed.should.be.true);
    it('should register one ignored consumer', () => context.ignoredConsumer.subscriberCount.should.equal(1));
    it('should register one ignored listener', () => context.ignoredConsumer.listenerCount.should.equal(1));
    it('should observe ignored recommendations with the current route arguments', () => context.ignoredSubscriptions[0][1]!.should.deep.equal(context.params));
    it('should show the ignored query', () => context.table.query.should.equal(context.ignoredQuery));
    it('should clear the previous selection', () => context.table.selectedItems!.should.have.lengthOf(0));
}));
