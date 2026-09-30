// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { layOutThroughput, ThroughputPoint } from '../throughput';

describe('when laying out an hour', () => {
    const now = new Date('2026-09-30T12:34:56Z');
    let points: ThroughputPoint[];

    beforeEach(() => {
        points = layOutThroughput('hour', now, [
            [
                { from: '2026-09-30T12:34:00Z', count: 3 },
                { from: '2026-09-30T11:36:00Z', count: 2 },
                { from: '2026-09-30T11:00:00Z', count: 100 }
            ],
            [
                { from: '2026-09-30T12:34:00Z', count: 4 }
            ]
        ]);
    });

    it('should have one point per minute', () => points.length.should.equal(60));
    it('should start the window at the oldest minute it covers', () => points[0].from.toISOString().should.equal('2026-09-30T11:35:00.000Z'));
    it('should end the window at the current minute', () => points[59].from.toISOString().should.equal('2026-09-30T12:34:00.000Z'));
    it('should add up the counts of every histogram in the same minute', () => points[59].count.should.equal(7));
    it('should place a bucket in its own minute', () => points[1].count.should.equal(2));
    it('should fill the minutes without events with zero', () => points[30].count.should.equal(0));
    it('should leave out buckets from before the window', () => points.reduce((sum, _) => sum + _.count, 0).should.equal(9));
});
