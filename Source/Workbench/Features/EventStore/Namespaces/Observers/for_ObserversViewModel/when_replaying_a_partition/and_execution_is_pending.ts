// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult } from '@cratis/arc/commands';
import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when replaying a partition and execution is pending', given(a_view_model, context => {
    let wasPending: boolean;
    let wasEligible: boolean;

    beforeEach(async () => {
        let complete!: (result: CommandResult) => void;
        let started!: () => void;
        const executing = new Promise<void>(resolve => { started = resolve; });
        context.execute.callsFake(() => {
            started();
            return new Promise<CommandResult>(resolve => { complete = resolve; });
        });
        const request = context.request();
        await executing;
        wasPending = context.viewModel.isReplayingPartition;
        wasEligible = context.viewModel.canReplayPartition;
        await context.request();
        complete(CommandResult.empty);
        await request;
    });

    it('should hold the pending state', () => wasPending.should.be.true);
    it('should disable submission', () => wasEligible.should.be.false);
    it('should confirm once', () => context.dialogs.showConfirmation.should.be.calledOnce);
    it('should execute once', () => context.execute.should.be.calledOnce);
    it('should release the pending state', () => context.viewModel.isReplayingPartition.should.be.false);
}));
