// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when replaying a partition and the operator retries', given(a_view_model, context => {
    let feedbackAtConfirmation: string;

    beforeEach(async () => {
        context.execute.onFirstCall().resolves(CommandResult.failed(['Temporary failure']));
        await context.request();
        context.dialogs.showConfirmation.callsFake(async () => {
            feedbackAtConfirmation = context.viewModel.partitionReplayFeedback;
            return DialogResult.Yes;
        });
        await context.request();
    });

    it('should clear stale feedback before confirming', () => feedbackAtConfirmation.should.equal(''));
    it('should execute the retry', () => context.execute.should.be.calledTwice);
    it('should show acceptance', () => context.viewModel.partitionReplayAccepted.should.be.true);
}));
