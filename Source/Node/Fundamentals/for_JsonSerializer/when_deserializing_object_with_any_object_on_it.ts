// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { field } from '../fieldDecorator';
import { JsonSerializer } from '../JsonSerializer';


class TheType {
    @field(Object)
    anyObject!: any;
}

const json = '{' +
    '   "anyObject": {' +
    '      "someNumber": 42,' +
    '      "someString": "forty two",' +
    '      "someBoolean": true' +
    '   }' +
    '}';

describe('when deserializing object with any object on it', () => {
    const result = JsonSerializer.deserialize(TheType, json);

    it('should hold correct number for first instance', () => result.anyObject.someNumber.should.equal(42));
    it('should hold correct string for first instance', () => result.anyObject.someString.should.equal('forty two'));
    it('should hold correct type for first instance', () => result.anyObject.someBoolean.should.equal(true));
});
