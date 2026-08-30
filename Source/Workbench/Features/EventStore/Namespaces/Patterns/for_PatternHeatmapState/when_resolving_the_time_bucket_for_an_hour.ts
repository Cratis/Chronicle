// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { timeBucketForHour } from '../PatternHeatmapState';

describe('when resolving the time bucket for an hour', () => {
    it('should put the small hours at night', () => timeBucketForHour(2).should.equal('Night'));
    it('should start the early morning at five', () => timeBucketForHour(5).should.equal('EarlyMorning'));
    it('should put nine in the morning', () => timeBucketForHour(9).should.equal('Morning'));
    it('should put twelve at midday', () => timeBucketForHour(12).should.equal('Midday'));
    it('should put fifteen in the afternoon', () => timeBucketForHour(15).should.equal('Afternoon'));
    it('should put nineteen in the evening', () => timeBucketForHour(19).should.equal('Evening'));
    it('should put twenty three at night', () => timeBucketForHour(23).should.equal('Night'));
});
