// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when managing selection and toggling a recommendation on', given(a_view_model, (context) => {
    beforeAll(() => context.viewModel.toggleRecommendationSelection(context.firstRecommendationId));

    it('should mark the recommendation as selected', () => context.viewModel.isRecommendationSelected(context.firstRecommendationId).should.be.true);
    it('should hold exactly one selected id', () => context.viewModel.selectedRecommendationIds.length.should.equal(1));
}));
