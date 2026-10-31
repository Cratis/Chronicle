// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { ObserverOwner, ObserverRunningState } from 'Features/Contracts/Observation';
import { given } from 'given';
import { a_view_model } from '../when_replaying/given/a_view_model';

describe('when fast forwarding and the selected observer is replaying', given(a_view_model, (context) => {
    beforeEach(async () => {
        context.viewModel.selectedObserver = new ObserverInformation();
        context.viewModel.selectedObserver.owner = ObserverOwner.client;
        context.viewModel.selectedObserver.runningState = ObserverRunningState.replaying;
        await context.viewModel.fastForward(context.params.eventStore!, context.params.namespace!);
    });

    it('should not allow fast forward', () => context.viewModel.canFastForward.should.be.false);
    it('should not perform fast forward', () => context.fastForward.execute.should.not.be.called);
}));
