// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Fields } from '../Fields';

class MyType {
}

class MyOtherType {
}

describe('when adding fields to type', () => {
    const firstField = "firstField";
    const secondField = "secondField";
    const thirdField = "thirdField";

    const firstFieldType = String;
    const secondFieldType = Number;
    const thirdFieldType = MyOtherType;

    Fields.addFieldToType(MyType, firstField, firstFieldType);
    Fields.addFieldToType(MyType, secondField, secondFieldType);
    Fields.addFieldToType(MyType, thirdField, thirdFieldType);

    const fields = Fields.getFieldsForType(MyType);

    it('should hold first field', () => fields[0].name.should.equal(firstField));
    it('should have correct type for first field', () => fields[0].type.should.equal(firstFieldType));
    it('should hold second field', () => fields[1].name.should.equal(secondField));
    it('should have correct type for second field', () => fields[1].type.should.equal(secondFieldType));
    it('should hold third field', () => fields[2].name.should.equal(thirdField));
    it('should have correct type for third field', () => fields[2].type.should.equal(thirdFieldType));
});
