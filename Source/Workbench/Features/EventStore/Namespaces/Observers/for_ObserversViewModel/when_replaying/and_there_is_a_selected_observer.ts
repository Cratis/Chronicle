// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when replaying and there is a selected observer', given(a_view_model, (context) => {
    beforeEach(async () => {
        context.viewModel.selectedObserver = new ObserverInformation();
        context.viewModel.selectedObserver.eventSequenceId = 'inbox-source';
        context.dialogs.showConfirmation.resolves(DialogResult.Yes);
        await context.viewModel.replay(context.params.eventStore!, context.params.namespace!);
    });

    it('should display dialog', () => context.dialogs.showConfirmation.should.be.called);
    it('should perform replay', () => context.replay.execute.should.be.called);
    it('should replay the selected observers sequence', () => context.replay.eventSequenceId.should.equal('inbox-source'));
}));
