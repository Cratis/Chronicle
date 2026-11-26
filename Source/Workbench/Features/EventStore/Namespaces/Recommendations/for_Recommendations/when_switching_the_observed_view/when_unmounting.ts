// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_recommendations_page } from '../given/a_recommendations_page';

describe('when unmounting the recommendations page', given(a_recommendations_page, context => {
    beforeEach(async () => context.root.render(null));

    it('should release the active consumer', () => context.activeConsumer.subscriberCount.should.equal(0));
    it('should remove the active listener', () => context.activeConsumer.listenerCount.should.equal(0));
}));
