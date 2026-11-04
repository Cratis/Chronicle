// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ChangeUserPassword } from '../ChangeUserPassword';
import { GetStatus } from '../GetStatus';
import { SetInitialAdminPassword } from '../SetInitialAdminPassword';
import { LoginViewModel } from '../LoginViewModel';

describe('when cancelling initial setup', () => {
    let viewModel: LoginViewModel;

    beforeEach(() => {
        viewModel = new LoginViewModel(
            new ChangeUserPassword(),
            new SetInitialAdminPassword(),
            sinon.createStubInstance(GetStatus),
        );
        viewModel.setupToken = 'token-from-the-server-log';
        viewModel.cancelPasswordChange();
    });

    it('should clear the setup token', () => viewModel.setupToken.should.equal(''));
});
