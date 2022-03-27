// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { SomeCommand } from './SomeCommand';

describe('when property changes and initial value is undefined', () => {
    const command = new SomeCommand();
    command.someProperty = '42';
    command.propertyChanged('someProperty');

    it('should have changes', () => command.hasChanges.should.be.true);
});
