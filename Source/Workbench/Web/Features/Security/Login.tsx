// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { withViewModel } from '@cratis/arc.react.mvvm';
import { LoginViewModel } from './LoginViewModel';
import { InputText } from 'primereact/inputtext';
import { Password } from 'primereact/password';
import { Button } from 'primereact/button';
import { Message } from 'primereact/message';
import { ProgressSpinner } from 'primereact/progressspinner';
import css from './Login.module.css';
import chronicleLogo from './chronicle.svg';
import { useEffect } from 'react';

export const Login = withViewModel(LoginViewModel, ({ viewModel }) => {
    useEffect(() => {
        viewModel.checkInitialSetup();
    }, []);

    const getSubtitle = () => {
        if (viewModel.isInitialSetup) {
            return 'Set Admin Password';
        }
        if (viewModel.requiresPasswordChange) {
            return 'Change Your Password';
        }
        return 'Sign in to continue';
    };

    const getButtonLabel = () => {
        if (viewModel.isLoggingIn) {
            return viewModel.isInitialSetup ? 'Setting...' : 'Changing...';
        }
        return viewModel.isInitialSetup ? 'Set Password' : 'Change Password';
    };

    const sliderStateClass = viewModel.requiresPasswordChange ? css.showChangePassword : css.showLogin;

    if (viewModel.isLoading) {
        return (
            <div className={css.loginContainer}>
                <div className={css.loginCard}>
                    <div className={css.loginHeader}>
                        <img src={chronicleLogo} alt="Chronicle" className={css.loginLogo} style={{ filter: 'brightness(0) invert(1)' }} />
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'center', padding: '2rem' }}>
                        <ProgressSpinner style={{ width: '50px', height: '50px' }} />
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className={css.loginContainer}>
            <div className={css.loginCard}>
                <div className={css.loginHeader}>
                    <img src={chronicleLogo} alt="Chronicle" className={css.loginLogo} style={{ filter: 'brightness(0) invert(1)' }} />
                    <p className={css.loginSubtitle}>
                        {getSubtitle()}
                    </p>
                </div>

                <div className={`${css.formSlider} ${sliderStateClass}`}>
                    <div className={css.formTrack}>
                        <form className={`${css.loginForm} ${css.signInPane}`} onSubmit={(e) => { e.preventDefault(); viewModel.login(); }}>
                            <div className={css.formGroup}>
                                <label htmlFor="username" className={css.label}>Username</label>
                                <InputText
                                    id="username"
                                    value={viewModel.username}
                                    onChange={(e) => viewModel.username = e.target.value}
                                    className={css.input}
                                    placeholder="Enter your username"
                                    autoFocus={!viewModel.requiresPasswordChange}
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

                        <form className={`${css.loginForm} ${css.changePasswordPane}`} onSubmit={(e) => { e.preventDefault(); viewModel.changePassword(); }}>
                            {viewModel.isInitialSetup && (
                                <p className={css.initialSetupMessage}>
                                    Welcome! Please set a password for the admin account to continue.
                                </p>
                            )}

                            <div className={css.formGroup}>
                                <label htmlFor="newPassword" className={css.label}>
                                    {viewModel.isInitialSetup ? 'Password' : 'New Password'}
                                </label>
                                <Password
                                    id="newPassword"
                                    value={viewModel.newPassword}
                                    onChange={(e) => viewModel.newPassword = e.target.value}
                                    className={css.input}
                                    inputClassName={css.passwordInput}
                                    panelClassName={css.passwordPanel}
                                    appendTo="self"
                                    placeholder={viewModel.isInitialSetup ? 'Enter password' : 'Enter new password'}
                                    toggleMask
                                    autoFocus={viewModel.requiresPasswordChange}
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
                                    placeholder="Confirm password"
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
                                    label={getButtonLabel()}
                                    className={css.loginButton}
                                    loading={viewModel.isLoggingIn}
                                    disabled={viewModel.isLoggingIn || !viewModel.newPassword || !viewModel.confirmPassword}
                                />
                                {!viewModel.isInitialSetup && (
                                    <Button
                                        type="button"
                                        label="Cancel"
                                        severity="secondary"
                                        onClick={() => viewModel.cancelPasswordChange()}
                                        disabled={viewModel.isLoggingIn}
                                    />
                                )}
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    );
});
