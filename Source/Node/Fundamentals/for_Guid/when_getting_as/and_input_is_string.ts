// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '../../Guid';

class MyType extends Guid {}

describe('when getting as and input is string', () => {
    const guidAsString = '0be23d5b-90d6-45f4-94fb-f1537caeea73';
    const result = Guid.as<MyType>(guidAsString);

    it('should return a guid with the expected identifier', () => result.toString().should.equal(guidAsString));
});
