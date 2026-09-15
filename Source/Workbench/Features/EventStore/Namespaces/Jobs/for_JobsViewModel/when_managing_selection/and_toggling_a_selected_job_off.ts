// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when managing selection and toggling a selected job off', given(a_view_model, (context) => {
    beforeAll(() => {
        context.viewModel.toggleJobSelection(context.firstJobId);
        context.viewModel.toggleJobSelection(context.firstJobId);
    });

    it('should no longer mark the job as selected', () => context.viewModel.isJobSelected(context.firstJobId).should.be.false);
    it('should hold no selected ids', () => context.viewModel.selectedJobIds.length.should.equal(0));
}));
