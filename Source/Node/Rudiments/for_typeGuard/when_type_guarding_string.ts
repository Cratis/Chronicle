// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { typeGuard } from '../typeGuard';

describe('when type guarding string', () => {
    const value: any = 'some string';
    const type_guarded_string = typeGuard(value, 'string');

    it('should be a string', () => type_guarded_string.should.be.true);
});
