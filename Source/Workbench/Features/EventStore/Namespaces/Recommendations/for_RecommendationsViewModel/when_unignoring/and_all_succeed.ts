// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from 'given';
import { a_view_model_with_ignored_recommendations } from './given/a_view_model_with_ignored_recommendations';

describe('when unignoring and all succeed', given(a_view_model_with_ignored_recommendations, (context) => {
    beforeAll(async () => {
        context.answerWith(undefined);
        await context.unignore();
    });
    afterAll(() => sinon.restore());

    it('should report success', () => context.outcome.should.deep.equal({ total: 2, failed: 0, failures: [] }));
    it('should unignore one recommendation per item', () => context.commands.length.should.equal(2));
    it('should target the first recommendation', () => context.commands[0].recommendationId.equals(context.firstRecommendationId).should.be.true);
    it('should target the second recommendation', () => context.commands[1].recommendationId.equals(context.secondRecommendationId).should.be.true);
    it('should use the event store of the page', () => context.commands[0].eventStore.should.equal('some-event-store'));
    it('should use the namespace of the page', () => context.commands[0].namespace.should.equal('some-namespace'));
    it('should clear the selection', () => context.viewModel.selectedRecommendations.length.should.equal(0));
}));
