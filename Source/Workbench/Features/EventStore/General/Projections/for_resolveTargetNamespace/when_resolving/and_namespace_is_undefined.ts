// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { resolveTargetNamespace } from '../../resolveTargetNamespace';

describe('when resolving and namespace is undefined', () => {
    let result: string;

    beforeEach(() => {
        result = resolveTargetNamespace(undefined);
    });

    it('should return default namespace', () => {
        result.should.equal('Default');
    });
});
