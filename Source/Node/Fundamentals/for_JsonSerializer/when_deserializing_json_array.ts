// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { field } from '../fieldDecorator';
import { JsonSerializer } from '../JsonSerializer';


class TheType {
    @field(Number)
    someNumber!: number;

    @field(String)
    someString!: string;

    @field(Date)
    someDate!: Date;
}

const json = '[{' +
    '    "someNumber": 42,' +
    '    "someString": "forty two",' +
    '    "someDate": "2022-10-07 15:51"' +
    '}, {' +
    '    "someNumber": 43,' +
    '    "someString": "forty three",' +
    '    "someDate": "2022-10-07 14:42"' +
    '}]';

describe('when deserializing json array', () => {
    const result = JsonSerializer.deserializeArray(TheType, json);

    it('should return an array with 2 items', () => result.length.should.equal(2));

    it('should hold correct number for first instance', () => result[0].someNumber.should.equal(42));
    it('should hold correct string for first instance', () => result[0].someString.should.equal('forty two'));
    it('should hold correct type for first instance', () => result[0].someDate.constructor.should.equal(Date));
    it('should hold correct value for first instance', () => result[0].someDate.toString().should.equal(new Date('2022-10-07 15:51').toString()));


    it('should hold correct number for first instance', () => result[1].someNumber.should.equal(43));
    it('should hold correct string for first instance', () => result[1].someString.should.equal('forty three'));
    it('should hold correct type for first instance', () => result[1].someDate.constructor.should.equal(Date));
    it('should hold correct value for first instance', () => result[1].someDate.toString().should.equal(new Date('2022-10-07 14:42').toString()));
});
