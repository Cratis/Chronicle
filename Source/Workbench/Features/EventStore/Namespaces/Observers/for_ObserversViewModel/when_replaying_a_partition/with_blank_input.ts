// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

for (const input of ['', ' \t\n ']) {
    describe(`when replaying a partition with blank input '${input}'`, given(a_view_model, context => {
        beforeEach(async () => {
            context.viewModel.partitionEventSourceId = input;
            await context.request();
        });

        it('should be ineligible', () => context.viewModel.canReplayPartition.should.be.false);
        it('should explain the required input', () => context.viewModel.partitionReplayFeedback.should.contain('Enter'));
        it('should not confirm', () => context.dialogs.showConfirmation.should.not.be.called);
        it('should not execute', () => context.execute.should.not.be.called);
    }));
}
