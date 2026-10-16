// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_namespace_choice } from './given/a_namespace_choice';
import { resolveNamespace } from '../../resolveNamespace';

describe('when resolving namespace and the route has a namespace', given(a_namespace_choice, (context) => {
    let result: string;

    beforeEach(() => {
        result = resolveNamespace('Production', 'Staging', context.available);
    });

    it('should resolve to the route namespace', () => {
        result.should.equal('Production');
    });
}));
