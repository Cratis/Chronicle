// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { clearAntiforgeryToken, getAntiforgeryHeaders, refreshAntiforgeryToken } from '../antiforgery';

describe('when authentication is disabled', () => {
    let sandbox: sinon.SinonSandbox;
    let enabled: boolean;
    let originalLocation: PropertyDescriptor | undefined;

    beforeEach(async () => {
        sandbox = sinon.createSandbox();
        originalLocation = Object.getOwnPropertyDescriptor(globalThis, 'location');
        Object.defineProperty(globalThis, 'location', { configurable: true, value: new URL('https://chronicle.example/') });
        const fetchStub = sandbox.stub(globalThis, 'fetch');
        fetchStub.onFirstCall().resolves(new Response(JSON.stringify({ requestToken: 'old-session-token' })));
        fetchStub.onSecondCall().resolves(new Response(null, { status: 204 }));
        await refreshAntiforgeryToken();
        enabled = await refreshAntiforgeryToken();
    });

    afterEach(() => {
        sandbox.restore();
        clearAntiforgeryToken();
        if (originalLocation) Object.defineProperty(globalThis, 'location', originalLocation);
        else Reflect.deleteProperty(globalThis, 'location');
    });

    it('should report disabled protection', () => enabled.should.be.false);
    it('should discard any previous session token', () => getAntiforgeryHeaders().should.deep.equal({}));
});
