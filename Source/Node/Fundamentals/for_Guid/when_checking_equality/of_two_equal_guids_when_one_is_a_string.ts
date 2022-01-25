// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '../../Guid';

describe('when checking equality of two equal guids when one is a string', () => {
    const guid1 = Guid.parse('8c9dad7d-7c4e-4d32-ac89-44561b0c3bb1');
    const guid2 = '8c9dad7d-7c4e-4d32-ac89-44561b0c3bb1';

    it('should be the same', () => guid1.equals(guid2).should.be.true);
});
