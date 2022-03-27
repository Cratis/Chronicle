// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SomeCommand } from './SomeCommand';

describe('when setting initial values after property changed', () => {
    const command = new SomeCommand();
    command.setInitialValues({
        someProperty: ''
    });
    command.someProperty = '42';
    command.propertyChanged('someProperty');

    command.setInitialValuesFromCurrentValues();

    it('should not have any changes', () => command.hasChanges.should.be.false);
});
