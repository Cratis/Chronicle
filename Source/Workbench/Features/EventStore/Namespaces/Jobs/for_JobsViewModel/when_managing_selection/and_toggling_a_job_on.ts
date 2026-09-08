// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when managing selection and toggling a job on', given(a_view_model, (context) => {
    beforeAll(() => context.viewModel.toggleJobSelection(context.firstJobId));

    it('should mark the job as selected', () => context.viewModel.isJobSelected(context.firstJobId).should.be.true);
    it('should hold exactly one selected id', () => context.viewModel.selectedJobIds.length.should.equal(1));
}));
