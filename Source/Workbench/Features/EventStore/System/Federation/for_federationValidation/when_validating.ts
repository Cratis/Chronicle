// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { formatBytes, formatList, isValidHttpsEndpoint, isValidPin, parseList } from '../federationValidation';

const validPin = `sha256:${'A'.repeat(43)}=`;

describe('when validating federation input', () => {
    it('should accept a well formed pin', () => isValidPin(validPin).should.be.true);
    it('should reject a pin without the algorithm prefix', () => isValidPin('A'.repeat(44)).should.be.false);
    it('should reject a pin of the wrong length', () => isValidPin(`sha256:${'A'.repeat(20)}=`).should.be.false);
    it('should reject an empty pin', () => isValidPin('').should.be.false);
    it('should accept an https endpoint', () => isValidHttpsEndpoint('https://peer.example.com:35000').should.be.true);
    it('should reject an http endpoint', () => isValidHttpsEndpoint('http://peer.example.com').should.be.false);
    it('should reject text that is not a URL', () => isValidHttpsEndpoint('peer').should.be.false);
    it('should split, trim and de-duplicate a list', () => parseList(' a, b,a ,, c\nd').should.deep.equal(['a', 'b', 'c', 'd']));
    it('should format a list', () => formatList(['a', 'b']).should.equal('a, b'));
    it('should format small byte counts', () => formatBytes(512).should.equal('512 B'));
    it('should format larger byte counts', () => formatBytes(1536).should.equal('1.5 KiB'));
});
