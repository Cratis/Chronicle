// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { ObserverRunningState } from 'Features/Contracts/Observation';
import { ObserverRemovalOutcome } from 'Features/Concepts/Observation';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from '../when_replaying/given/a_view_model';

/**
 * A refusal comes back as a successful command that declined, so nothing surfaces it unless the view model does - and
 * a listing that refreshes either way looks identical whether the observer went or stayed. The observer also has to
 * stay selected, because the operator's next move is to try again once the declaring application is stopped.
 */
describe('when removing an observer and the kernel refuses because a client is subscribed', given(a_view_model, (context) => {
    beforeEach(async () => {
        context.dialogs.showConfirmation.resolves(DialogResult.Yes);
        context.removeObserverSucceedsWith(ObserverRemovalOutcome.observerSubscribed, 'the-busy-namespace');
        context.viewModel.selectedObserver = new ObserverInformation();
        context.viewModel.selectedObserver.id = 'the-observer';
        context.viewModel.selectedObserver.runningState = ObserverRunningState.disconnected;
        await context.viewModel.removeObserver(context.params.eventStore!, context.params.namespace!);
    });

    it('should tell the operator it was refused', () => context.dialogs.showConfirmation.should.be.calledTwice);
    it('should name the namespace that blocked it', () => context.dialogs.showConfirmation.secondCall.args[1].should.contain('the-busy-namespace'));
    it('should keep the observer selected so the operator can try again', () => context.viewModel.selectedObserver!.id.should.equal('the-observer'));
}));
