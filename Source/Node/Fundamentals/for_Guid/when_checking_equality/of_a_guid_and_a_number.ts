// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '../../Guid';

describe('when checking equality a guid and a number', () => {
    const guid = Guid.parse('1a0a061b-3533-4d91-990a-524b7e6ae6a9');
    const num = 2;

    it('should not be the same', () => guid.equals(num).should.be.false);
});
