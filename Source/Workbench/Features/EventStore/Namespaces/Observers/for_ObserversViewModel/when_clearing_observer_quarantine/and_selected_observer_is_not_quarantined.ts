// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation, ObserverRunningState } from 'Api/Observation';
import { given } from 'given';
import { a_view_model } from '../when_replaying/given/a_view_model';

describe('when clearing observer quarantine and selected observer is not quarantined', given(a_view_model, (context) => {
    beforeEach(() => {
        context.viewModel.selectedObserver = new ObserverInformation();
        context.viewModel.selectedObserver.runningState = ObserverRunningState.active;
        context.viewModel.clearObserverQuarantine();
    });

    it('should indicate that quarantine cannot be cleared', () => context.viewModel.canClearObserverQuarantine.should.be.false);
    it('should not perform clear quarantine', () => context.clearObserverQuarantine.execute.should.not.be.called);
}));
