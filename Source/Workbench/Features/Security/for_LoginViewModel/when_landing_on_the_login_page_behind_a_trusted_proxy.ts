// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ChangeUserPassword } from '../ChangeUserPassword';
import { GetStatus } from '../GetStatus';
import { SetInitialAdminPassword } from '../SetInitialAdminPassword';
import { LoginViewModel } from '../LoginViewModel';

describe('when landing on the login page behind a trusted proxy', () => {
    let sandbox: sinon.SinonSandbox;
    let originalLocation: PropertyDescriptor | undefined;
    let originalStorage: PropertyDescriptor | undefined;
    let assign: sinon.SinonStub;
    let store: Record<string, string>;
    let viewModel: LoginViewModel;

    const arrive = async () => {
        viewModel = new LoginViewModel(new ChangeUserPassword(), new SetInitialAdminPassword(), sinon.createStubInstance(GetStatus));
        await viewModel.checkInitialSetup();
    };

    beforeEach(() => {
        sandbox = sinon.createSandbox();
        originalLocation = Object.getOwnPropertyDescriptor(globalThis, 'location');
        originalStorage = Object.getOwnPropertyDescriptor(globalThis, 'sessionStorage');
        assign = sandbox.stub();
        store = {};
        Object.defineProperty(globalThis, 'location', { configurable: true, value: { origin: 'https://chronicle.example', assign } });
        Object.defineProperty(globalThis, 'sessionStorage', {
            configurable: true,
            value: { getItem: (k: string) => store[k] ?? null, setItem: (k: string, v: string) => { store[k] = v; } }
        });
        sandbox.stub(globalThis, 'fetch').callsFake(() => Promise.resolve(new Response(JSON.stringify({ mode: 'trustedProxy' }))));
    });

    afterEach(() => {
        sandbox.restore();
        if (originalLocation) Object.defineProperty(globalThis, 'location', originalLocation);
        else Reflect.deleteProperty(globalThis, 'location');
        if (originalStorage) Object.defineProperty(globalThis, 'sessionStorage', originalStorage);
        else Reflect.deleteProperty(globalThis, 'sessionStorage');
    });

    describe('for the first time', () => {
        beforeEach(arrive);

        it('should send the user through the proxy without a click', () => assign.calledOnceWith('/').should.be.true);
        it('should keep the spinner up while the browser navigates', () => viewModel.isLoading.should.be.true);
    });

    describe('again right after being sent through the proxy', () => {
        beforeEach(async () => {
            await arrive();
            assign.resetHistory();
            await arrive();
        });

        it('should not redirect again', () => assign.called.should.be.false);
        it('should show the message instead', () => viewModel.isLoading.should.be.false);
    });

    describe('after the window has passed', () => {
        beforeEach(async () => {
            store['chronicle.trustedProxy.autoSignIn'] = (Date.now() - 120_000).toString();
            await arrive();
        });

        it('should send the user through the proxy again', () => assign.calledOnceWith('/').should.be.true);
    });
});
