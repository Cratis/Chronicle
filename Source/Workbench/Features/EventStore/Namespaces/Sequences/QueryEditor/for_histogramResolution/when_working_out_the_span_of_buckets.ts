// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { spanOf } from '../histogramResolution';

describe('when working out the span of buckets', () => {
    const span = spanOf([
        { start: 300, end: 400, count: 2 },
        { start: 100, end: 200, count: 5 }
    ]);

    it('should start at the earliest bucket', () => span!.min.should.equal(100));
    it('should end at the latest bucket', () => span!.max.should.equal(400));
});

describe('when there are no buckets', () => {
    it('should have no span to pick a range within', () => (spanOf([]) === undefined).should.be.true);
});
