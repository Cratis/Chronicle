// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SomeCommand } from './SomeCommand';

describe('when property has changed and we revert', () => {
    const command = new SomeCommand();
    command.setInitialValues({
        someProperty: ''
    });
    command.someProperty = '42';
    command.revertChanges();

    it('should not have any changes', () => command.hasChanges.should.be.false);
});
