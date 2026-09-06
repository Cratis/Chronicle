// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { colorFor, luminanceOf } from '../heatmapScale';

describe('when resolving a color for a value', () => {
    it('should give the bottom of the scale for zero', () => colorFor(0).should.eql({ red: 68, green: 1, blue: 84 }));
    it('should give the top of the scale for one', () => colorFor(1).should.eql({ red: 253, green: 231, blue: 37 }));
    it('should clamp below the bottom', () => colorFor(-2).should.eql(colorFor(0)));
    it('should clamp above the top', () => colorFor(4).should.eql(colorFor(1)));

    // Monotonic lightness is what makes the scale readable in grayscale and under color blindness, so it is the
    // property worth asserting rather than any particular color.
    it('should get lighter all the way up the scale', () => {
        const luminances = Array.from({ length: 20 }, (_, step) => luminanceOf(colorFor(step / 19)));
        const ascending = luminances.every((value, index) => index === 0 || value > luminances[index - 1]);
        ascending.should.be.true;
    });
});
