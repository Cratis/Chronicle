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

    it('should still attempt every recommendation', () => context.viewModel.commands.length.should.equal(2));
    it('should throw an aggregate error', () => context.error!.message.should.contain('Failed to unignore 1 of 2 recommendation(s)'));
    it('should name the failed recommendation', () => context.error!.message.should.contain(`${context.firstRecommendationId.toString()}: boom`));
    it('should not name the succeeded recommendation', () => context.error!.message.should.not.contain(context.secondRecommendationId.toString()));
    it('should clear the selection', () => context.viewModel.selectedRecommendationIds.length.should.equal(0));
}));
