// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_namespaces_store } from '../given/a_namespaces_store';

describe('when namespaces are pushed and the current namespace no longer exists', given(a_namespaces_store, (context) => {
    beforeEach(() => {
        context.store.setEventStore('the-store');
        context.pushNamespaces('Default', 'tenant-a');
        context.store.setCurrentNamespace('tenant-a');
        context.pushNamespaces('Default');
    });

    it('should settle on a namespace that exists', () => context.store.currentNamespace.value.should.equal('Default'));
}));
