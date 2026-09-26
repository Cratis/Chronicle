// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { RecommendationDetails } from 'Features/Recommendations';
import { a_view_model } from './given/a_view_model';

describe('when managing selection', given(a_view_model, (context) => {
    beforeEach(() => {
        context.viewModel.selectedRecommendations = [{ id: context.firstRecommendationId } as RecommendationDetails];
    });

    it('should hold the selection every action reads', () => context.viewModel.selectedRecommendations.should.have.lengthOf(1));

    it('should clear the selection when asked', () => {
        context.viewModel.clearRecommendationSelection();
        context.viewModel.selectedRecommendations.should.be.empty;
    });
}));
