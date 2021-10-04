// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { typeGuard } from '../typeGuard';

describe('when type guarding number', () => {
    const value: any = 1;
    const type_guarded_number = typeGuard(value, 'number');

    it('should be a number', () => type_guarded_number.should.be.true);
});
