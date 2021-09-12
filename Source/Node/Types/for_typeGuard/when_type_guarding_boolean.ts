// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { typeGuard } from '../typeGuard';

describe('when type guarding boolean', () => {
    const value: any = false;
    const type_guarded_boolean = typeGuard(value, 'boolean');

    it('should be a boolean', () => type_guarded_boolean.should.be.true);
});
