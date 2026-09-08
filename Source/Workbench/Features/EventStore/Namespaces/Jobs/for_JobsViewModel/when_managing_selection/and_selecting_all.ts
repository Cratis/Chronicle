// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when managing selection and selecting all', given(a_view_model, (context) => {
    beforeAll(() => context.viewModel.selectAllJobs([context.firstJobId, context.secondJobId]));

    it('should mark the first job as selected', () => context.viewModel.isJobSelected(context.firstJobId).should.be.true);
    it('should mark the second job as selected', () => context.viewModel.isJobSelected(context.secondJobId).should.be.true);
    it('should hold both selected ids', () => context.viewModel.selectedJobIds.length.should.equal(2));
}));
