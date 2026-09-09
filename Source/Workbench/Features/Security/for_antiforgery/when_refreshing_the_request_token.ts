// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon, { type SinonStub } from 'sinon';
import {
    clearAntiforgeryToken,
    getAntiforgeryHeaders,
    refreshAntiforgeryToken,
} from '../antiforgery';

describe('when refreshing the request token', () => {
    let fetchStub: SinonStub;
    let originalLocation: PropertyDescriptor | undefined;

    beforeEach(async () => {
        clearAntiforgeryToken();
        originalLocation = Object.getOwnPropertyDescriptor(globalThis, 'location');
        Object.defineProperty(globalThis, 'location', {
            configurable: true,
            value: new URL('https://chronicle.example/'),
        });
        fetchStub = sinon.stub(globalThis, 'fetch').resolves(
            new Response(JSON.stringify({ requestToken: 'the-token' }), {
                status: 200,
                headers: { 'Content-Type': 'application/json' },
            }),
        );

        await refreshAntiforgeryToken();
    });

    afterEach(() => {
        fetchStub.restore();
        clearAntiforgeryToken();
        if (originalLocation) {
            Object.defineProperty(globalThis, 'location', originalLocation);
        } else {
            Reflect.deleteProperty(globalThis, 'location');
        }
    });

    it('should request the token from the same origin', () =>
        (fetchStub.firstCall.args[0] as URL).origin.should.equal(
            'https://chronicle.example',
        ));
    it('should include credentials', () =>
        fetchStub.firstCall.args[1].credentials.should.equal('include'));
    it('should return the token as a command header', () =>
        getAntiforgeryHeaders().should.deep.equal({ 'X-CSRF-TOKEN': 'the-token' }));
});
