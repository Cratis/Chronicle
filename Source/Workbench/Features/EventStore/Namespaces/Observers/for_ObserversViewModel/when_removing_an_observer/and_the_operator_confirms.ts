// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { ObserverRunningState } from 'Features/Contracts/Observation';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from '../when_replaying/given/a_view_model';

describe('when removing an observer and the operator confirms', given(a_view_model, (context) => {
    beforeEach(async () => {
        context.dialogs.showConfirmation.resolves(DialogResult.Yes);
        context.viewModel.selectedObserver = new ObserverInformation();
        context.viewModel.selectedObserver.id = 'the-observer';
        context.viewModel.selectedObserver.runningState = ObserverRunningState.disconnected;
        await context.viewModel.removeObserver(context.params.eventStore!, context.params.namespace!);
    });

    it('should ask for confirmation first', () => context.dialogs.showConfirmation.should.be.called);
    it('should remove the observer', () => (context.removeObserver.execute as sinon.SinonStub).should.be.called);
    it('should remove the observer it was asked about', () => context.removeObserver.observerId!.should.equal('the-observer'));
    it('should clear the selection so the listing does not keep offering a removed observer', () => (context.viewModel.selectedObserver === undefined).should.be.true);
}));
