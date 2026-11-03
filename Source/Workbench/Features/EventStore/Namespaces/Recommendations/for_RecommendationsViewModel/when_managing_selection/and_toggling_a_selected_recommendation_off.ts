// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when reconciling selection after an observable refresh', given(a_view_model, context => {
    beforeAll(() => {
        context.viewModel.selectedRecommendations = context.recommendations;
        context.viewModel.reconcileSelection([context.recommendations[1]]);
    });
    it('should prune the removed recommendation and retain the surviving selection', () =>
        context.viewModel.selectedRecommendations.map(item => item.id).should.deep.equal([context.secondRecommendationId]));
}));
