// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

for (const dependency of ['confirmation', 'transport']) {
    describe(`when replaying a partition and ${dependency} throws`, given(a_view_model, context => {
        beforeEach(async () => {
            if (dependency === 'confirmation') context.dialogs.showConfirmation.rejects(new Error('Connection lost'));
            else context.execute.rejects(new Error('Connection lost'));
            await context.request();
        });

        it('should show the failure', () => context.viewModel.partitionReplayFeedback.should.contain('Connection lost'));
        it('should preserve the input', () => context.viewModel.partitionEventSourceId.should.equal(' Customer-A/1,Two '));
        it('should release the pending state', () => context.viewModel.isReplayingPartition.should.be.false);
        it('should permit retry', () => context.viewModel.canReplayPartition.should.be.true);
    }));
}
