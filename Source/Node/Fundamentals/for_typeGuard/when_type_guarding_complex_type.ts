// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { typeGuard } from '../typeGuard';

class some_complex_type {
    constructor(readonly x: number) { }
}
describe('when type guarding some complex type', () => {
    const value: any = new some_complex_type(2);
    const type_guarded_boolean = typeGuard(value, some_complex_type);

    it('should be some complex type', () => type_guarded_boolean.should.be.true);
});
