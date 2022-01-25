// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '../Guid';

describe('when getting empty', () => {
    const empty = Guid.empty;

    it('should return a guid with 16 bytes all set to 0', () => {
        empty.bytes.should.be.lengthOf(16);
        empty.bytes.some((_: any) => _ !== 0).should.be.false;
    });
});
