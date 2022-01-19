// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '../../Guid';

class MyType extends Guid {}

describe('when getting as and input is specific type', () => {
    const guidAsString = '0be23d5b-90d6-45f4-94fb-f1537caeea73';
    const input = MyType.parse(guidAsString) as MyType;
    const result = Guid.as<MyType>(input);

    it('should return what was given', () => result.should.equal(input));
});
