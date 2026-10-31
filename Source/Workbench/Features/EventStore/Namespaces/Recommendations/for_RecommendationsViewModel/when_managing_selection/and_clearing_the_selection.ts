// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when managing selection and clearing the selection', given(a_view_model, context => {
    beforeAll(() => {
        context.viewModel.selectedRecommendations = context.recommendations;
        context.viewModel.selectedRecommendations = [];
    });
    it('should hold no selected recommendations', () => context.viewModel.selectedRecommendations.should.have.lengthOf(0));
}));
