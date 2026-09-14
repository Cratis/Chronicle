// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon, { type SinonStub } from 'sinon';
import {
    clearAntiforgeryToken,
    getAntiforgeryHeaders,
    refreshAntiforgeryToken,
} from '../antiforgery';

describe('when the request token response is invalid', () => {
    let fetchStub: SinonStub;
    let originalLocation: PropertyDescriptor | undefined;
    let caughtError: unknown;

    beforeEach(async () => {
        clearAntiforgeryToken();
        originalLocation = Object.getOwnPropertyDescriptor(globalThis, 'location');
        Object.defineProperty(globalThis, 'location', {
            configurable: true,
            value: new URL('https://chronicle.example/'),
        });
        fetchStub = sinon.stub(globalThis, 'fetch').resolves(
            new Response(JSON.stringify({}), {
                status: 200,
                headers: { 'Content-Type': 'application/json' },
            }),
        );

        try {
            await refreshAntiforgeryToken();
        } catch (error) {
            caughtError = error;
        }
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

    it('should fail', () => (caughtError instanceof Error).should.be.true);
    it('should not retain a command header', () =>
        getAntiforgeryHeaders().should.deep.equal({}));
});
