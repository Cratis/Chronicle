// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from '../when_managing_selection/given/a_view_model';

describe('when toggling the ignored view and showing ignored', given(a_view_model, (context) => {
    beforeAll(() => {
        context.viewModel.selectAllRecommendations([context.firstRecommendationId, context.secondRecommendationId]);
        context.viewModel.toggleIgnoredView();
    });

    it('should show the ignored recommendations', () => context.viewModel.showIgnored.should.be.true);
    it('should clear the selection', () => context.viewModel.selectedRecommendationIds.length.should.equal(0));
}));
