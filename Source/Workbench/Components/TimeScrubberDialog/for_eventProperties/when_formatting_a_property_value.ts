// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { formatPropertyValue } from '../eventProperties';

describe('when formatting a property value', () => {
    it('should show a string as it is', () => formatPropertyValue('Acme').should.equal('Acme'));

    it('should show a number', () => formatPropertyValue(30330.88).should.equal('30330.88'));

    it('should show zero rather than treating it as absent', () => formatPropertyValue(0).should.equal('0'));

    it('should show a boolean', () => formatPropertyValue(false).should.equal('false'));

    it('should show an empty string as it is', () => formatPropertyValue('').should.equal(''));

    it('should mark null as absent', () => formatPropertyValue(null).should.equal('-'));

    it('should mark undefined as absent', () => formatPropertyValue(undefined).should.equal('-'));

    it('should render a nested object rather than [object Object]', () =>
        formatPropertyValue({ city: 'Oslo' }).should.equal('{"city":"Oslo"}'));

    it('should render an array', () => formatPropertyValue([1, 2]).should.equal('[1,2]'));
});
