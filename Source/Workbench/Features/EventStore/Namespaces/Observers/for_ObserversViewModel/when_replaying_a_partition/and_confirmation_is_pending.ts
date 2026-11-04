// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when replaying a partition and confirmation is pending', given(a_view_model, context => {
    let confirm!: (result: DialogResult) => void;
    let request: Promise<void>;
    let wasPending: boolean;

    beforeEach(async () => {
        context.dialogs.showConfirmation.returns(new Promise<DialogResult>(resolve => { confirm = resolve; }));
        request = context.request();
        wasPending = context.viewModel.isReplayingPartition;
        context.viewModel.selectedObserver!.id = 'another-observer';
        context.viewModel.selectedObserver!.eventSequenceId = 'another-sequence';
        context.viewModel.partitionEventSourceId = 'another-source';
        await context.request();
        confirm(DialogResult.Yes);
        await request;
    });

    it('should hold the pending state', () => wasPending.should.be.true);
    it('should confirm only once', () => context.dialogs.showConfirmation.should.be.calledOnce);
    it('should execute only once', () => context.execute.should.be.calledOnce);
    it('should keep the confirmed observer', () => context.replayPartition.observerId.should.equal('selected-observer'));
    it('should keep the confirmed sequence', () => context.replayPartition.eventSequenceId.should.equal('inbox-source'));
    it('should keep the confirmed event source', () => context.replayPartition.partition.should.equal(' Customer-A/1,Two '));
    for (const target of ['selected-observer', ' Customer-A/1,Two ', 'eventStore', 'namespace']) {
        it(`should confirm the target ${target}`, () => (context.dialogs.showConfirmation.firstCall.args[1] as string).should.contain(target));
    }
}));
