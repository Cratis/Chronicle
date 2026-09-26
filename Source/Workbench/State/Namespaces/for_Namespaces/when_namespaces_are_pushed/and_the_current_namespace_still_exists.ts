// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_namespaces_store } from '../given/a_namespaces_store';

describe('when namespaces are pushed and the current namespace still exists', given(a_namespaces_store, (context) => {
    beforeEach(() => {
        context.store.setEventStore('the-store');
        context.pushNamespaces('Default', 'tenant-a');
        context.store.setRouteNamespace('tenant-a');
        context.store.setCurrentNamespace('tenant-a');
        context.pushNamespaces('Default', 'tenant-a', 'tenant-b');
    });

    it('should leave the current namespace alone', () => context.store.currentNamespace.value.should.equal('tenant-a'));
}));
