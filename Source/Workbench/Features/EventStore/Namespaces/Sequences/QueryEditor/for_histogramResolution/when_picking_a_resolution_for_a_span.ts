// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolutionForSpan } from '../histogramResolution';

const minute = 60 * 1000;
const hour = 60 * minute;
const day = 24 * hour;

describe('when picking a resolution for a span', () => {
    it('should use minutes for an hour', () => resolutionForSpan(hour).should.equal('minute'));
    it('should use hours for a day', () => resolutionForSpan(day).should.equal('hour'));
    it('should use days for a month', () => resolutionForSpan(30 * day).should.equal('day'));
    it('should use weeks for a year', () => resolutionForSpan(365 * day).should.equal('week'));
    it('should use months for a decade', () => resolutionForSpan(3650 * day).should.equal('month'));
    it('should use minutes for an empty span', () => resolutionForSpan(0).should.equal('minute'));
});
