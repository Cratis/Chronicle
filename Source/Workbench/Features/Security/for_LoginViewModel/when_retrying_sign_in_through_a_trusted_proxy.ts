// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ChangeUserPassword } from '../ChangeUserPassword';
import { GetStatus } from '../GetStatus';
import { SetInitialAdminPassword } from '../SetInitialAdminPassword';
import { LoginViewModel } from '../LoginViewModel';

describe('when retrying sign in through a trusted proxy', () => {
    let originalLocation: PropertyDescriptor | undefined;
    let assign: sinon.SinonStub;
    let viewModel: LoginViewModel;

    beforeEach(() => {
        originalLocation = Object.getOwnPropertyDescriptor(globalThis, 'location');
        assign = sinon.stub();
        Object.defineProperty(globalThis, 'location', { configurable: true, value: { origin: 'https://chronicle.example', assign } });
        viewModel = new LoginViewModel(new ChangeUserPassword(), new SetInitialAdminPassword(), sinon.createStubInstance(GetStatus));
        viewModel.retrySignIn();
    });

    afterEach(() => {
        if (originalLocation) Object.defineProperty(globalThis, 'location', originalLocation);
        else Reflect.deleteProperty(globalThis, 'location');
    });

    it('should send the user back through the proxy at the root', () => assign.calledOnceWith('/').should.be.true);
});
