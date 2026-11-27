// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { formatPosition } from '../observerLagPresentation';

describe('when formatting a position', () => {
    it('should show zero, which is a real position', () => formatPosition(0)!.should.equal('0'));
    it('should show a safe integer', () => formatPosition(42)!.should.equal('42'));
    it('should show the largest safe integer', () => formatPosition(Number.MAX_SAFE_INTEGER)!.should.equal('9007199254740991'));
    it('should not show a number JavaScript can only have rounded', () => (formatPosition(Number.MAX_SAFE_INTEGER + 1) === undefined).should.be.true);
    it('should not show the rounded top-end sentinel', () => (formatPosition(2 ** 64) === undefined).should.be.true);
    it('should not show a negative number', () => (formatPosition(-1) === undefined).should.be.true);
    it('should not show a fraction', () => (formatPosition(1.5) === undefined).should.be.true);
    it('should not show NaN', () => (formatPosition(Number.NaN) === undefined).should.be.true);
    it('should not show a missing value', () => {
        (formatPosition(undefined) === undefined).should.be.true;
        (formatPosition(null) === undefined).should.be.true;
    });
});
