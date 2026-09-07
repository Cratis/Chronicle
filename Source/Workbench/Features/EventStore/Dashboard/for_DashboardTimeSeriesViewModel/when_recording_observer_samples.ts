// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DashboardTimeSeriesViewModel } from '../DashboardTimeSeriesViewModel';
import { type ObserverRunningStateBreakdown } from '../ObserverRunningStateBreakdown';

const breakdown = (active: number, suspended: number): ObserverRunningStateBreakdown => ({
    active,
    suspended,
    replaying: 0,
    disconnected: 0,
    quarantined: 0,
    unknown: 0
});

describe('when recording an observer sample', () => {
    let viewModel: DashboardTimeSeriesViewModel;

    beforeEach(() => {
        viewModel = new DashboardTimeSeriesViewModel();
        viewModel.recordObserverSample(breakdown(5, 2));
    });

    it('should record the active count', () => viewModel.observerSamples[0].active.should.equal(5));
    it('should record the total count across all states', () => viewModel.observerSamples[0].total.should.equal(7));
});
