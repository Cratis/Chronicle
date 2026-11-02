// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when managing selection and selecting one recommendation', given(a_view_model, context => {
    beforeAll(() => { context.viewModel.selectedRecommendations = [context.recommendations[0]]; });
    it('should hold exactly that recommendation', () => context.viewModel.selectedRecommendations.map(item => item.id).should.deep.equal([context.firstRecommendationId]));
}));
