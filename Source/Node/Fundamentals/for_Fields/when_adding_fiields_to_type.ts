// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Fields } from '../Fields';

class MyType {
}

class MyOtherType {
}

class MyThirdType {
}

describe('when adding fields to type', () => {
    const firstField = "firstField";
    const secondField = "secondField";
    const thirdField = "thirdField";

    const firstFieldType = String;
    const secondFieldType = Number;
    const thirdFieldType = MyOtherType;

    Fields.addFieldToType(MyType, firstField, firstFieldType, false, []);
    Fields.addFieldToType(MyType, secondField, secondFieldType, false, [MyOtherType, MyThirdType]);
    Fields.addFieldToType(MyType, thirdField, thirdFieldType, true, []);

    const fields = Fields.getFieldsForType(MyType);

    it('should hold first field', () => fields[0].name.should.equal(firstField));
    it('should have correct type for first field', () => fields[0].type.should.equal(firstFieldType));
    it('should not consider first field as enumerable', () => fields[0].enumerable.should.be.false);
    it('should not have any derivatives for first field', () => fields[0].derivatives.should.be.empty);
    it('should hold second field', () => fields[1].name.should.equal(secondField));
    it('should have correct type for second field', () => fields[1].type.should.equal(secondFieldType));
    it('should not consider second field as enumerable', () => fields[1].enumerable.should.be.false);
    it('should have 2 derivatives for second field', () => fields[1].derivatives.length.should.equal(2));
    it('should hold third field', () => fields[2].name.should.equal(thirdField));
    it('should have correct type for third field', () => fields[2].type.should.equal(thirdFieldType));
    it('should consider third field as enumerable', () => fields[2].enumerable.should.be.true);
    it('should not have any derivatives for third field', () => fields[2].derivatives.should.be.empty);
});
