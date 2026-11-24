// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { FastForwardOutcome, FastForwardResult } from 'Features/Concepts/Observation';
import { ObserverOwner, ObserverRunningState } from 'Features/Contracts/Observation';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import sinon from 'sinon';
import { a_view_model } from '../when_replaying/given/a_view_model';

describe('when fast forwarding and the fast forward is accepted as a job', given(a_view_model, (context) => {
    beforeEach(async () => {
        const result = new FastForwardResult();
        result.outcome = FastForwardOutcome.accepted;
        context.fastForward.execute = sinon.stub().resolves({ isSuccess: true, response: result, onException: sinon.stub() });
        context.dialogs.showConfirmation.resolves(DialogResult.Yes);
        context.viewModel.selectedObserver = new ObserverInformation();
        context.viewModel.selectedObserver.id = 'the-observer';
        context.viewModel.selectedObserver.owner = ObserverOwner.client;
        context.viewModel.selectedObserver.runningState = ObserverRunningState.active;
        await context.viewModel.fastForward(context.params.eventStore!, context.params.namespace!);
    });

    it('should tell the operator that the fast forward was accepted', () => context.dialogs.showConfirmation.secondCall.args[1].should.contain('was accepted'));
}));
