// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from './given/a_view_model';

for (const result of [DialogResult.No, DialogResult.Cancelled, DialogResult.None]) {
    describe(`when replaying a partition and the operator declines with ${result}`, given(a_view_model, context => {
        beforeEach(async () => {
            context.dialogs.showConfirmation.resolves(result);
            await context.request();
        });

        it('should not execute', () => context.execute.should.not.be.called);
        it('should release the pending state', () => context.viewModel.isReplayingPartition.should.be.false);
        it('should allow retry', () => context.viewModel.canReplayPartition.should.be.true);
    }));
}
