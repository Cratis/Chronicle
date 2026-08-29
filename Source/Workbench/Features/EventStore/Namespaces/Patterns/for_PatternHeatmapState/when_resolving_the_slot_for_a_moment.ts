// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { slotFor } from '../PatternHeatmapState';

describe('when resolving the slot for a moment', () => {
    // The days start on Monday because that is how a working week reads, while getDay() starts on Sunday.
    const monday = new Date(2026, 7, 24, 9, 30);
    const sunday = new Date(2026, 7, 23, 9, 30);

    it('should name the day the week starts on', () => slotFor(monday).day.should.equal('Monday'));
    it('should name the part of the day', () => slotFor(monday).timeBucket.should.equal('Morning'));
    it('should put a sunday at the end of the week', () => slotFor(sunday).day.should.equal('Sunday'));
});
