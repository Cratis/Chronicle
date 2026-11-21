// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { AuthenticationMode, getAuthenticationMode } from '../authenticationMode';

describe('when the server predates the endpoint', () => {
    let sandbox: sinon.SinonSandbox;
    let originalLocation: PropertyDescriptor | undefined;
    let result: AuthenticationMode;

    beforeEach(async () => {
        sandbox = sinon.createSandbox();
        originalLocation = Object.getOwnPropertyDescriptor(globalThis, 'location');
        Object.defineProperty(globalThis, 'location', { configurable: true, value: new URL('https://chronicle.example/') });
        sandbox.stub(globalThis, 'fetch').resolves(new Response(null, { status: 404 }));
        result = await getAuthenticationMode();
    });

    afterEach(() => {
        sandbox.restore();
        if (originalLocation) Object.defineProperty(globalThis, 'location', originalLocation);
        else Reflect.deleteProperty(globalThis, 'location');
    });

    it('should fall back to chronicle sign-in', () => result.mode.should.equal('chronicle'));
});
