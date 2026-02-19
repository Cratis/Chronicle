// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation } from 'Api/Observation';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when replaying and there is a selected observer', given(a_view_model, (context) => {
    beforeEach(() => {
        context.viewModel.selectedObserver = new ObserverInformation();
        context.dialogs.showConfirmation.resolves(DialogResult.Yes);
        context.viewModel.replay();
    });

    it('should display dialog', () => context.dialogs.showConfirmation.should.be.called);
    it('should perform replay', () => context.replay.execute.should.be.called);
}));
