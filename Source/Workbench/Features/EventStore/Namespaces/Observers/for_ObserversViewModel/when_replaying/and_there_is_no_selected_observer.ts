// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from 'given';
import { a_view_model } from './given/a_view_model';

describe('when replaying and there is no selected observer', given(a_view_model, (context) => {
    beforeEach(() => context.viewModel.replay());

    it('should not display any dialog', () => context.dialogs.showConfirmation.should.not.be.called);
    it('should not perform replay', () => context.replay.execute.should.not.be.called);
}));
