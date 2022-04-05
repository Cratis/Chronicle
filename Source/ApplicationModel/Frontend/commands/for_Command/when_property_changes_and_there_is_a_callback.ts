// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { SomeCommand } from './SomeCommand';

describe('when property changes and there is a callback', () => {
    const command = new SomeCommand();
    const callback = sinon.stub();
    command.onPropertyChanged(callback, command);

    command.propertyChanged('someProperty');

    it('should call the callback', () => callback.calledWith('someProperty').should.be.true);
});
