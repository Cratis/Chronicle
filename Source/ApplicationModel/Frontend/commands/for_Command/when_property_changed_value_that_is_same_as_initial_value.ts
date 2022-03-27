// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SomeCommand } from './SomeCommand';

describe('when property changed value that is same as initial value', () => {
    const command = new SomeCommand();
    command.setInitialValues({
        someProperty: ''
    });

    command.someProperty = '';
    command.propertyChanged('someProperty');

    it('should have no changes', () => command.hasChanges.should.be.false);
});
