// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveTargetNamespace } from '../../resolveTargetNamespace';

describe('when resolving and namespace is defined', () => {
    let result: string;

    beforeEach(() => {
        result = resolveTargetNamespace('MyNamespace');
    });

    it('should return the provided namespace', () => {
        result.should.equal('MyNamespace');
    });
});
