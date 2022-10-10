// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { field } from '../fieldDecorator';
import { JsonSerializer } from '../JsonSerializer';
import { Constructor } from '../Constructor';

class OtherType {
    @field(Number)
    someNumber: number;

    @field(String)
    someString: string;

    @field(Date)
    someDate: Date;

    @field(Date)
    collectionOfDates: Date[];

    @field(Number)
    collectionOfNumbers: number[];
}

class TopLevel {
    @field(Number)
    someNumber: number;

    @field(String)
    someString: string;

    @field(Date)
    someDate: Date;

    @field(OtherType)
    otherType: OtherType;

    @field(OtherType, true)
    collectionOfOtherType: OtherType[];
}

const json = '{' +
    '    "someNumber": 42,' +
    '    "someString": "forty two",' +
    '    "someDate": "2022-10-07 15:51",' +
    '    "otherType": {' +
    '       "someNumber": 43,' +
    '       "someString": "forty three",' +
    '       "someDate": "2022-11-07 15:51"' +
    '    },' +
    '    "collectionOfOtherTypes": ['+
    '       {' +
    '           "someNumber": 44,' +
    '           "someString": "forty four",' +
    '           "someDate": "2022-12-07 15:51"' +
    '       },' +
    '       {' +
    '           "someNumber": 45,' +
    '           "someString": "forty five",' +
    '           "someDate": "2022-13-07 15:51"' +
    '       }' +
    '    ]'+
    '}';



describe('when deserializing complex nested object with multiple wellknown types', () => {
    const result = JsonSerializer.deserialize(TopLevel, json);

    it('should hold correct number for first level number', () => result.someNumber.should.equal(42));
    it('should hold correct string for first level string', () => result.someString.should.equal('forty two'));
    it('should hold correct type for first level date', () => result.someDate.constructor.should.equal(Date));
    it('should hold correct value for first level date', () => result.someDate.toString().should.equal(new Date('2022-10-07 15:51').toString()));

    it('should hold correct number for second level number', () => result.otherType.someNumber.should.equal(43));
    it('should hold correct string for second level string', () => result.otherType.someString.should.equal('forty three'));
    it('should hold correct type for second level date', () => result.otherType.someDate.constructor.should.equal(Date));
    it('should hold correct value for second level date', () => result.otherType.someDate.toString().should.equal(new Date('2022-11-07 15:51').toString()));

    it('should have 2 items in the children', () => result.collectionOfOtherType.length.should.equal(2));

});
