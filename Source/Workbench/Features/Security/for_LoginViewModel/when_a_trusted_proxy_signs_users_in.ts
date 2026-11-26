// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ChangeUserPassword } from '../ChangeUserPassword';
import { GetStatus } from '../GetStatus';
import { SetInitialAdminPassword } from '../SetInitialAdminPassword';
import { LoginViewModel } from '../LoginViewModel';

describe('when a trusted proxy signs users in', () => {
    let sandbox: sinon.SinonSandbox;
    let originalLocation: PropertyDescriptor | undefined;
    let viewModel: LoginViewModel;
    let getStatus: sinon.SinonStubbedInstance<GetStatus>;

    beforeEach(async () => {
        sandbox = sinon.createSandbox();
        originalLocation = Object.getOwnPropertyDescriptor(globalThis, 'location');
        Object.defineProperty(globalThis, 'location', { configurable: true, value: { origin: 'https://chronicle.example', assign: sandbox.stub() } });
        Object.defineProperty(globalThis, 'sessionStorage', { configurable: true, value: { getItem: () => String(Date.now()), setItem: () => {} } });
        sandbox.stub(globalThis, 'fetch').resolves(new Response(JSON.stringify({ mode: 'trustedProxy', signOutUrl: '/oauth2/sign_out' })));
        getStatus = sinon.createStubInstance(GetStatus);
        viewModel = new LoginViewModel(new ChangeUserPassword(), new SetInitialAdminPassword(), getStatus);
        await viewModel.checkInitialSetup();
    });

    afterEach(() => {
        sandbox.restore();
        Reflect.deleteProperty(globalThis, 'sessionStorage');
        if (originalLocation) Object.defineProperty(globalThis, 'location', originalLocation);
        else Reflect.deleteProperty(globalThis, 'location');
    });

    it('should recognize the trusted proxy', () => viewModel.isTrustedProxy.should.be.true);
    it('should not ask for the admin password setup status', () => getStatus.perform.called.should.be.false);
    it('should not enter the initial setup flow', () => viewModel.isInitialSetup.should.be.false);
    it('should finish loading', () => viewModel.isLoading.should.be.false);
});
