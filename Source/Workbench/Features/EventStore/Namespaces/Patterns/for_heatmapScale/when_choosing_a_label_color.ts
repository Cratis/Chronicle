// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { colorFor, labelColorFor } from '../heatmapScale';

describe('when choosing a label color', () => {
    it('should be light on the dark end of the scale', () => labelColorFor(colorFor(0)).should.equal('#ffffff'));
    it('should be dark on the bright end of the scale', () => labelColorFor(colorFor(1)).should.equal('#101010'));
    it('should be light in the middle, which is still dark', () => labelColorFor(colorFor(0.5)).should.equal('#ffffff'));
});
