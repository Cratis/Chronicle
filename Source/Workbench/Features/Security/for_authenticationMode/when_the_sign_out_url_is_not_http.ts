// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { AuthenticationMode, getAuthenticationMode } from '../authenticationMode';

describe('when the sign-out url is not http', () => {
    let sandbox: sinon.SinonSandbox;
    let originalLocation: PropertyDescriptor | undefined;
    let result: AuthenticationMode;

    beforeEach(async () => {
        sandbox = sinon.createSandbox();
        originalLocation = Object.getOwnPropertyDescriptor(globalThis, 'location');
        Object.defineProperty(globalThis, 'location', { configurable: true, value: new URL('https://chronicle.example/') });
        sandbox.stub(globalThis, 'fetch').resolves(new Response(JSON.stringify({ mode: 'trustedProxy', signOutUrl: 'javascript:alert(1)' })));
        result = await getAuthenticationMode();
    });

    afterEach(() => {
        sandbox.restore();
        if (originalLocation) Object.defineProperty(globalThis, 'location', originalLocation);
        else Reflect.deleteProperty(globalThis, 'location');
    });

    it('should still report the trusted proxy', () => result.mode.should.equal('trustedProxy'));
    it('should drop the sign-out url', () => (result.signOutUrl === undefined).should.be.true);
});
