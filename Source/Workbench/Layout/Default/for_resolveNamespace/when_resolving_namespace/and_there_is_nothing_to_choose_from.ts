// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_namespace_choice } from './given/a_namespace_choice';
import { resolveNamespace } from '../../resolveNamespace';

describe('when resolving namespace and there is nothing to choose from', given(a_namespace_choice, () => {
    let result: string;

    beforeEach(() => {
        result = resolveNamespace(undefined, undefined, []);
    });

    it('should resolve to an empty namespace', () => {
        result.should.equal('');
    });
}));
