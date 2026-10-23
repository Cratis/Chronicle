// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Features/Observation';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when replaying and the replay is not started', given(a_view_model, (context) => {
    beforeEach(async () => {
        context.viewModel.selectedObserver = new ObserverInformation();
        context.viewModel.selectedObserver.eventSequenceId = 'inbox-source';
        context.dialogs.showConfirmation.resolves(DialogResult.Yes);
        await context.viewModel.replay(context.params.eventStore!, context.params.namespace!);
        context.replayResult.onValidationFailure.firstCall.args[0]([{ message: 'Replay could not be started for this observer.' }]);
    });

    it('should tell the user why the replay was not started', () =>
        (context.dialogs.showConfirmation.secondCall.args[1] as string).should.contain('Replay could not be started for this observer.'));
}));
