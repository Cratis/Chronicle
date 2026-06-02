// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObserverInformation, ObserverRunningState } from 'Api/Observation';
import { given } from 'given';
import { a_view_model } from '../when_replaying/given/a_view_model';

describe('when clearing observer quarantine and there is a selected quarantined observer', given(a_view_model, (context) => {
    beforeEach(() => {
        context.viewModel.selectedObserver = new ObserverInformation();
        context.viewModel.selectedObserver.runningState = ObserverRunningState.quarantined;
        context.viewModel.clearObserverQuarantine();
    });

    it('should indicate that quarantine can be cleared', () => context.viewModel.canClearObserverQuarantine.should.be.true);
    it('should perform clear quarantine', () => context.clearObserverQuarantine.execute.should.be.called);
}));
