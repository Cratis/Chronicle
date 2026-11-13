// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from '../given/a_view_model';

describe('when reindexing and the operator declines', given(a_view_model, context => {
    beforeEach(async () => {
        context.dialogs.showConfirmation.resolves(DialogResult.No);
        await context.request();
    });

    it('should not execute', () => context.execute.should.not.have.been.called);
    it('should not offer a job link', () => (context.viewModel.jobId === undefined).should.be.true);
    it('should not report acceptance', () => context.viewModel.feedback.should.equal(''));
    it('should release the pending state', () => context.viewModel.isReindexing.should.be.false);
}));
