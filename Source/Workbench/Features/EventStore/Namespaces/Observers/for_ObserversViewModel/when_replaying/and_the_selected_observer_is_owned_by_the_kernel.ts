// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { ObserverOwner } from 'Features/Contracts/Observation';
import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when replaying and the selected observer is owned by the kernel', given(a_view_model, (context) => {
    beforeEach(() => {
        const observer = new ObserverInformation();
        observer.owner = ObserverOwner.kernel;
        context.viewModel.selectedObserver = observer;
        context.viewModel.replay(context.params.eventStore!, context.params.namespace!);
    });

    it('should not consider the observer replayable', () => context.viewModel.canReplay.should.be.false);
    it('should not display any dialog', () => context.dialogs.showConfirmation.should.not.be.called);
    it('should not perform replay', () => context.replay.execute.should.not.be.called);
}));
