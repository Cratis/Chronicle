// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '../../Guid';

describe('when checking equality of two different guids', () => {
    const guid1 = Guid.parse('1a0a061b-3533-4d91-990a-524b7e6ae6a9');
    const guid2 = Guid.parse('5262a2bf-bf1a-4af5-a96f-83f455581dec');

    it('should not be the same', () => guid1.equals(guid2).should.not.be.true);
});
