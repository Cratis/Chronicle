// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/arc.react.mvvm';
import { LoginViewModel } from './LoginViewModel';
import { InputText } from 'primereact/inputtext';
import { Password } from 'primereact/password';
import { Button } from 'primereact/button';
import { Message } from 'primereact/message';
import css from './Login.module.css';
import chronicleLogo from './chronicle.svg';

export const Login = withViewModel(LoginViewModel, ({ viewModel }) => {
    return (
        <div className={css.loginContainer}>
            <div className={css.loginCard}>
                <div className={css.loginHeader}>
                    <img src={chronicleLogo} alt="Chronicle" className={css.loginLogo} style={{ filter: 'brightness(0) invert(1)' }} />
                    <p className={css.loginSubtitle}>
                        {viewModel.requiresPasswordChange ? 'Change Your Password' : 'Sign in to continue'}
                    </p>
                </div>

                {!viewModel.requiresPasswordChange ? (
                    <form className={css.loginForm} onSubmit={(e) => { e.preventDefault(); viewModel.login(); }}>
                        <div className={css.formGroup}>
                            <label htmlFor="username" className={css.label}>Username</label>
                            <InputText
                                id="username"
                                value={viewModel.username}
                                onChange={(e) => viewModel.username = e.target.value}
                                className={css.input}
                                placeholder="Enter your username"
                                autoFocus
                                disabled={viewModel.isLoggingIn}
                            />
                        </div>

                        <div className={css.formGroup}>
                            <label htmlFor="password" className={css.label}>Password</label>
                            <Password
                                id="password"
                                value={viewModel.password}
                                onChange={(e) => viewModel.password = e.target.value}
                                className={css.input}
                                inputClassName={css.passwordInput}
                                placeholder="Enter your password"
                                feedback={false}
                                toggleMask
                                disabled={viewModel.isLoggingIn}
                            />
                        </div>

                        {viewModel.errorMessage && (
                            <Message
                                severity="error"
                                text={viewModel.errorMessage}
                                className={css.errorMessage}
                            />
                        )}

                        <Button
                            type="submit"
                            label={viewModel.isLoggingIn ? 'Signing in...' : 'Sign In'}
                            className={css.loginButton}
                            loading={viewModel.isLoggingIn}
                            disabled={viewModel.isLoggingIn || !viewModel.username || !viewModel.password}
                        />
                    </form>
                ) : (
                    <form className={css.loginForm} onSubmit={(e) => { e.preventDefault(); viewModel.changePassword(); }}>
                        <div className={css.formGroup}>
                            <label htmlFor="newPassword" className={css.label}>New Password</label>
                            <Password
                                id="newPassword"
                                value={viewModel.newPassword}
                                onChange={(e) => viewModel.newPassword = e.target.value}
                                className={css.input}
                                inputClassName={css.passwordInput}
                                placeholder="Enter new password"
                                toggleMask
                                autoFocus
                                disabled={viewModel.isLoggingIn}
                            />
                        </div>

                        <div className={css.formGroup}>
                            <label htmlFor="confirmPassword" className={css.label}>Confirm Password</label>
                            <Password
                                id="confirmPassword"
                                value={viewModel.confirmPassword}
                                onChange={(e) => viewModel.confirmPassword = e.target.value}
                                className={css.input}
                                inputClassName={css.passwordInput}
                                placeholder="Confirm new password"
                                feedback={false}
                                toggleMask
                                disabled={viewModel.isLoggingIn}
                            />
                        </div>

                        {viewModel.errorMessage && (
                            <Message
                                severity="error"
                                text={viewModel.errorMessage}
                                className={css.errorMessage}
                            />
                        )}

                        <div className={css.buttonGroup}>
                            <Button
                                type="submit"
                                label={viewModel.isLoggingIn ? 'Changing...' : 'Change Password'}
                                className={css.loginButton}
                                loading={viewModel.isLoggingIn}
                                disabled={viewModel.isLoggingIn || !viewModel.newPassword || !viewModel.confirmPassword}
                            />
                            <Button
                                type="button"
                                label="Cancel"
                                severity="secondary"
                                onClick={() => viewModel.cancelPasswordChange()}
                                disabled={viewModel.isLoggingIn}
                            />
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
});
