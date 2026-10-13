// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { ObserverOwner, ObserverRunningState } from 'Features/Contracts/Observation';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from '../when_replaying/given/a_view_model';

describe('when fast forwarding and there is a selected observer', given(a_view_model, (context) => {
    beforeEach(async () => {
        context.dialogs.showConfirmation.resolves(DialogResult.Yes);
        context.viewModel.selectedObserver = new ObserverInformation();
        context.viewModel.selectedObserver.id = 'the-observer';
        context.viewModel.selectedObserver.owner = ObserverOwner.client;
        context.viewModel.selectedObserver.runningState = ObserverRunningState.active;
        await context.viewModel.fastForward(context.params.eventStore!, context.params.namespace!);
    });

    it('should indicate that the observer can be fast forwarded', () => context.viewModel.canFastForward.should.be.true);
    it('should perform fast forward', () => context.fastForward.execute.should.be.called);
    it('should fast forward the selected observer', () => context.fastForward.observerId.should.equal('the-observer'));
}));
