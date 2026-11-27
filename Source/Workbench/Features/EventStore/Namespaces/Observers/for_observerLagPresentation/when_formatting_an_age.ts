// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { formatAge } from '../observerLagPresentation';

describe('when formatting an age', () => {
    it('should show seconds below a minute', () => formatAge(45).should.equal('45s'));
    it('should show minutes and seconds below an hour', () => formatAge(200).should.equal('3m 20s'));
    it('should show hours and minutes from an hour', () => formatAge(7500).should.equal('2h 5m'));
});
