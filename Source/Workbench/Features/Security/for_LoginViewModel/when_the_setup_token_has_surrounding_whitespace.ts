// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { QueryResult } from '@cratis/arc/queries';
import { CommandResult } from '@cratis/arc/commands';
import { ChangeUserPassword } from '../ChangeUserPassword';
import { GetStatus } from '../GetStatus';
import { AdminPasswordStatus } from '../AdminPasswordStatus';
import { SetInitialAdminPassword } from '../SetInitialAdminPassword';
import { LoginViewModel } from '../LoginViewModel';

describe('when the setup token has surrounding whitespace', () => {
    let tokenWhenExecuted: string | undefined;

    beforeEach(async () => {
        const getStatus = sinon.createStubInstance(GetStatus);
        getStatus.perform.resolves(QueryResult.empty(Object.assign(new AdminPasswordStatus(), { isRequired: true })));
        const setInitialAdminPassword = new SetInitialAdminPassword();
        sinon.stub(setInitialAdminPassword, 'execute').callsFake(async () => {
            tokenWhenExecuted = setInitialAdminPassword.setupToken;
            return CommandResult.failed(['failed']);
        });
        const viewModel = new LoginViewModel(sinon.createStubInstance(ChangeUserPassword), setInitialAdminPassword, getStatus);
        await viewModel.checkInitialSetup();
        viewModel.setupToken = ' \tsetup-code\n ';
        viewModel.newPassword = 'new-password';
        viewModel.confirmPassword = 'new-password';
        await viewModel.changePassword();
    });

    it('should send the trimmed setup token', () => tokenWhenExecuted!.should.equal('setup-code'));
});
