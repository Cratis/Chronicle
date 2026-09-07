// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DashboardTimeSeriesViewModel, maxDashboardSamples } from '../DashboardTimeSeriesViewModel';

describe('when recording throughput samples', () => {
    let viewModel: DashboardTimeSeriesViewModel;

    beforeEach(() => {
        viewModel = new DashboardTimeSeriesViewModel();
        viewModel.recordThroughputSample(100);
        viewModel.recordThroughputSample(140);
    });

    it('should record zero for the first sample, with nothing to compare against', () => viewModel.throughputSamples[0].should.equal(0));
    it('should record the delta since the previous sample', () => viewModel.throughputSamples[1].should.equal(40));
});

describe('when the tail sequence number goes backwards', () => {
    let viewModel: DashboardTimeSeriesViewModel;

    beforeEach(() => {
        viewModel = new DashboardTimeSeriesViewModel();
        viewModel.recordThroughputSample(100);
        viewModel.recordThroughputSample(10);
    });

    it('should clamp the delta to zero rather than go negative', () => viewModel.throughputSamples[1].should.equal(0));
});

describe('when more than the sample limit has been recorded', () => {
    let viewModel: DashboardTimeSeriesViewModel;

    beforeEach(() => {
        viewModel = new DashboardTimeSeriesViewModel();
        for (let sequenceNumber = 0; sequenceNumber <= maxDashboardSamples + 5; sequenceNumber++) {
            viewModel.recordThroughputSample(sequenceNumber);
        }
    });

    it('should drop the oldest samples to stay within the limit', () => viewModel.throughputSamples.should.have.lengthOf(maxDashboardSamples));
});
