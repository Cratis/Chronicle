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

describe('when initial setup submits the setup token', () => {
    let viewModel: LoginViewModel;
    let setInitialAdminPassword: SetInitialAdminPassword;
    let changePassword: ChangeUserPassword;
    let setInitialExecute: sinon.SinonStub;
    let changeExecute: sinon.SinonStub;
    let tokenWhenExecuted: string | undefined;

    beforeEach(async () => {
        const getStatus = sinon.createStubInstance(GetStatus);
        getStatus.perform.resolves(QueryResult.empty(Object.assign(new AdminPasswordStatus(), { isRequired: true })));
        setInitialAdminPassword = new SetInitialAdminPassword();
        changePassword = new ChangeUserPassword();
        setInitialExecute = sinon.stub(setInitialAdminPassword, 'execute');
        changeExecute = sinon.stub(changePassword, 'execute');
        setInitialExecute.callsFake(async () => {
            tokenWhenExecuted = setInitialAdminPassword.setupToken;
            return CommandResult.failed(['failed']);
        });
        viewModel = new LoginViewModel(changePassword, setInitialAdminPassword, getStatus);
        await viewModel.checkInitialSetup();
        viewModel.setupToken = 'token-from-the-server-log';
        viewModel.newPassword = 'a-long-enough-password';
        viewModel.confirmPassword = 'a-long-enough-password';
        await viewModel.changePassword();
    });

    it('should send the setup token', () => tokenWhenExecuted!.should.equal('token-from-the-server-log'));
    it('should execute the initial setup command', () => setInitialExecute.calledOnce.should.be.true);
    it('should not execute the change password command', () => changeExecute.called.should.be.false);
});
