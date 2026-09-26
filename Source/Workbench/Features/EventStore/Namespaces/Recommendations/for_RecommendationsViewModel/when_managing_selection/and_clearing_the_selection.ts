// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when managing selection and clearing the selection', given(a_view_model, (context) => {
    beforeAll(() => {
        context.viewModel.selectAllRecommendations([context.firstRecommendationId, context.secondRecommendationId]);
        context.viewModel.clearRecommendationSelection();
    });

    it('should hold no selected ids', () => context.viewModel.selectedRecommendationIds.length.should.equal(0));
    it('should no longer mark the first recommendation as selected', () => context.viewModel.isRecommendationSelected(context.firstRecommendationId).should.be.false);
}));
