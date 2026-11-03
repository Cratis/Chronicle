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

describe('when changing a password after setup', () => {
    let viewModel: LoginViewModel;
    let setInitialAdminPassword: SetInitialAdminPassword;
    let changePassword: ChangeUserPassword;
    let setInitialExecute: sinon.SinonStub;
    let changeExecute: sinon.SinonStub;

    beforeEach(async () => {
        const getStatus = sinon.createStubInstance(GetStatus);
        getStatus.perform.resolves(QueryResult.empty(Object.assign(new AdminPasswordStatus(), { isRequired: false })));
        setInitialAdminPassword = new SetInitialAdminPassword();
        changePassword = new ChangeUserPassword();
        setInitialExecute = sinon.stub(setInitialAdminPassword, 'execute');
        changeExecute = sinon.stub(changePassword, 'execute');
        changeExecute.resolves(CommandResult.failed(['failed']));
        viewModel = new LoginViewModel(changePassword, setInitialAdminPassword, getStatus);
        await viewModel.checkInitialSetup();
        viewModel.setupToken = 'stale-token';
        viewModel.password = 'current-password-1';
        viewModel.newPassword = 'a-long-enough-password';
        viewModel.confirmPassword = 'a-long-enough-password';
        await viewModel.changePassword();
    });

    it('should execute the change password command', () => changeExecute.calledOnce.should.be.true);
    it('should not execute the initial setup command', () => setInitialExecute.called.should.be.false);
    it('should not send a setup token', () => ('setupToken' in changePassword).should.be.false);
});
