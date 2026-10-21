// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_namespace_choice } from './given/a_namespace_choice';
import { resolveNamespace } from '../../resolveNamespace';

describe('when resolving namespace and the list is still loading', given(a_namespace_choice, () => {
    let result: string;

    beforeEach(() => {
        result = resolveNamespace(undefined, 'Staging', []);
    });

    it('should resolve to the remembered namespace', () => {
        result.should.equal('Staging');
    });
}));
