// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { SinonStub } from 'sinon';
import { a_view_model } from '../when_syncing_namespace_from_route/given/a_view_model';

describe('when the namespace store changes the current namespace and it was not selected by the user', given(a_view_model, (context) => {
    beforeEach(() => context.currentNamespaceSubject.next('pushed-namespace'));

    it('should show the namespace', () => context.viewModel.currentNamespace.should.equal('pushed-namespace'));

    it('should not navigate', () => (context.props.onNamespaceSelected as SinonStub).should.not.be.called);
}));
