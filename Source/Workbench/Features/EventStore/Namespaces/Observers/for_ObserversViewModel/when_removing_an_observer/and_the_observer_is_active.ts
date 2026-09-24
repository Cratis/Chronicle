// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { ObserverRunningState } from 'Features/Contracts/Observation';
import { given } from 'given';
import { a_view_model } from '../when_replaying/given/a_view_model';

/**
 * The kernel refuses a running observer whatever the Workbench does, so this is not the guard - it only keeps the
 * action from being offered for the one case the listing can already rule out, rather than inviting an operator to
 * confirm something irreversible that is certain to be refused.
 */
describe('when removing an observer and the observer is active', given(a_view_model, (context) => {
    beforeEach(async () => {
        context.viewModel.selectedObserver = new ObserverInformation();
        context.viewModel.selectedObserver.id = 'the-observer';
        context.viewModel.selectedObserver.runningState = ObserverRunningState.active;
        await context.viewModel.removeObserver(context.params.eventStore!, context.params.namespace!);
    });

    it('should not offer removal', () => context.viewModel.canRemoveObserver.should.be.false);
    it('should not ask for confirmation', () => context.dialogs.showConfirmation.should.not.be.called);
    it('should not attempt the removal', () => (context.removeObserver.execute as sinon.SinonStub).should.not.be.called);
}));
