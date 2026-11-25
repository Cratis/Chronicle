// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_recommendations_page } from '../given/a_recommendations_page';

describe('when opening the active recommendation view', given(a_recommendations_page, context => {
    it('should register one active consumer', () => context.activeConsumer.subscriberCount.should.equal(1));
    it('should register one active listener', () => context.activeConsumer.listenerCount.should.equal(1));
    it('should observe only one query', () => context.cache.getDiagnosticsSnapshot().entries.should.have.lengthOf(1));
    it('should not subscribe to ignored recommendations', () => context.ignoredSubscriptions.should.have.lengthOf(0));
    it('should observe active recommendations with the current route arguments', () => context.activeSubscriptions[0][1]!.should.deep.equal(context.params));
    it('should show the active query', () => context.table.query.should.equal(context.activeQuery));
    it('should pass the current route arguments to the table', () => context.table.queryArguments!.should.deep.equal(context.params));
}));
