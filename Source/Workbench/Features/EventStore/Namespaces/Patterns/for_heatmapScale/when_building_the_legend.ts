// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { colorFor, legendStops } from '../heatmapScale';

describe('when building the legend', () => {
    it('should produce the requested number of stops', () => legendStops(5).length.should.equal(5));
    it('should start at the bottom of the scale', () => legendStops(5)[0].should.eql(colorFor(0)));
    it('should end at the top of the scale', () => legendStops(5)[4].should.eql(colorFor(1)));
    it('should still produce a stop when asked for one', () => legendStops(1).length.should.equal(1));
});
