// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when managing selection and selecting all', given(a_view_model, (context) => {
    beforeAll(() => context.viewModel.selectAllRecommendations([context.firstRecommendationId, context.secondRecommendationId]));

    it('should mark the first recommendation as selected', () => context.viewModel.isRecommendationSelected(context.firstRecommendationId).should.be.true);
    it('should mark the second recommendation as selected', () => context.viewModel.isRecommendationSelected(context.secondRecommendationId).should.be.true);
    it('should hold both selected ids', () => context.viewModel.selectedRecommendationIds.length.should.equal(2));
}));
