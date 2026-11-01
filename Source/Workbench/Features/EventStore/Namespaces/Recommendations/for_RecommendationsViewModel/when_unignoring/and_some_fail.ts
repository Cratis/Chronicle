// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from 'given';
import { a_view_model_with_ignored_recommendations } from './given/a_view_model_with_ignored_recommendations';

describe('when unignoring and some fail', given(a_view_model_with_ignored_recommendations, (context) => {
    beforeAll(async () => {
        context.answerWith({ index: 0, message: 'boom' });
        await context.unignore();
    });
    afterAll(() => sinon.restore());

    it('should still attempt every recommendation', () => context.commands.length.should.equal(2));
    it('should report a partial result', () => {
        context.outcome.total.should.equal(2);
        context.outcome.failed.should.equal(1);
    });
    it('should name the failed recommendation', () => context.outcome.failures[0].should.contain(`${context.firstRecommendationId.toString()}: boom`));
    it('should not name the succeeded recommendation', () => context.outcome.failures.join().should.not.contain(context.secondRecommendationId.toString()));
    it('should retain the failed selection', () => context.viewModel.selectedRecommendations.map(item => item.id).should.deep.equal([context.firstRecommendationId]));
}));
