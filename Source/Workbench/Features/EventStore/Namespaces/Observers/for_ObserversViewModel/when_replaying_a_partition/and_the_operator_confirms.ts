// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when replaying a partition and the operator confirms', given(a_view_model, context => {
    beforeEach(async () => {
        await context.request();
    });

    it('should execute once', () => context.execute.should.be.calledOnce);
    it('should target the event store', () => context.replayPartition.eventStore.should.equal('eventStore'));
    it('should target the namespace', () => context.replayPartition.namespace.should.equal('namespace'));
    it('should target the observer', () => context.replayPartition.observerId.should.equal('selected-observer'));
    it('should target the selected sequence', () => context.replayPartition.eventSequenceId.should.equal('inbox-source'));
    it('should preserve the opaque event source id', () => context.replayPartition.partition.should.equal(' Customer-A/1,Two '));
    it('should not replay all', () => context.replay.execute.should.not.be.called);
    it('should report only request acceptance', () => context.viewModel.partitionReplayFeedback.should.contain('accepted'));
    it('should release the pending state', () => context.viewModel.isReplayingPartition.should.be.false);
}));
