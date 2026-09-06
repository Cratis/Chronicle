// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { maximumBubbles, sampleBubbles } from '../sampleBubbles';

describe('when choosing which events get a bubble', () => {
    it('should draw nothing for an empty timeline', () => sampleBubbles(0, 0).length.should.equal(0));

    it('should draw the only event of a timeline of one', () => sampleBubbles(1, 0).should.eql([0]));

    it('should draw every event when they fit', () => sampleBubbles(5, 0).should.eql([0, 1, 2, 3, 4]));

    it('should draw every event right up to the limit', () =>
        sampleBubbles(maximumBubbles, 0).length.should.equal(maximumBubbles));

    it('should thin a timeline past the limit', () =>
        sampleBubbles(5000, 0).length.should.be.at.most(maximumBubbles + 1));

    it('should still start at the first event when thinned', () => sampleBubbles(5000, 0)[0].should.equal(0));

    it('should still end at the last event when thinned', () => {
        const drawn = sampleBubbles(5000, 0);
        drawn[drawn.length - 1].should.equal(4999);
    });

    it('should draw where the scrubber sits even when sampling would skip it', () =>
        sampleBubbles(5000, 1777).should.contain(1777));

    it('should keep what it draws in order', () => {
        const drawn = sampleBubbles(5000, 1777);
        drawn.should.eql([...drawn].sort((left, right) => left - right));
    });

    it('should not draw the same event twice', () => {
        const drawn = sampleBubbles(5000, 1777);
        new Set(drawn).size.should.equal(drawn.length);
    });

    it('should ignore a position outside the timeline', () =>
        sampleBubbles(5000, 99999).every(index => index <= 4999).should.be.true);
});
