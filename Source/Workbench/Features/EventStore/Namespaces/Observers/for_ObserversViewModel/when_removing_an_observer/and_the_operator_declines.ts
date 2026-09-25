// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { ObserverRunningState } from 'Features/Contracts/Observation';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from '../when_replaying/given/a_view_model';

/**
 * Removal is irreversible, so the confirmation is the last point at which it can be called off. Anything other than
 * an explicit yes has to leave the observer alone.
 */
describe('when removing an observer and the operator declines', given(a_view_model, (context) => {
    beforeEach(async () => {
        context.dialogs.showConfirmation.resolves(DialogResult.No);
        context.viewModel.selectedObserver = new ObserverInformation();
        context.viewModel.selectedObserver.id = 'the-observer';
        context.viewModel.selectedObserver.runningState = ObserverRunningState.disconnected;
        await context.viewModel.removeObserver(context.params.eventStore!, context.params.namespace!);
    });

    it('should not remove the observer', () => (context.removeObserver.execute as sinon.SinonStub).should.not.be.called);
    it('should keep the observer selected', () => context.viewModel.selectedObserver!.id.should.equal('the-observer'));
}));
