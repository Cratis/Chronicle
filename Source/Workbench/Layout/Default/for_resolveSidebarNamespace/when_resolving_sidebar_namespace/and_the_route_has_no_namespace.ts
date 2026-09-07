// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_sidebar_namespace } from './given/a_sidebar_namespace';
import { resolveSidebarNamespace } from '../../resolveSidebarNamespace';

describe('when resolving sidebar namespace and the route has no namespace', given(a_sidebar_namespace, (context) => {
    let result: string;

    beforeEach(() => {
        result = resolveSidebarNamespace(undefined, context.previousNamespace);
    });

    it('should return the previous namespace', () => {
        result.should.equal(context.previousNamespace);
    });
}));
