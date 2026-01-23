// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { Guid } from '@cratis/fundamentals';
import { ChangePasswordForUser, SetInitialAdminPassword } from 'Api/Security';

@injectable()
export class LoginViewModel {
    username: string = '';
    password: string = '';
    newPassword: string = '';
    confirmPassword: string = '';
    isLoggingIn: boolean = false;
    errorMessage: string = '';
    requiresPasswordChange: boolean = false;
    isInitialSetup: boolean = false;
    userId: Guid | null = null;

    constructor(
        readonly _changePassword: ChangePasswordForUser,
        readonly _setInitialAdminPassword: SetInitialAdminPassword) {
    }

    async login() {
        this.isLoggingIn = true;
        this.errorMessage = '';

        try {
            const response = await fetch('/api/security/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    username: this.username,
                    password: this.password,
                }),
                credentials: 'include',
            });

            if (response.ok) {
                const result = await response.json();

                if (result.requiresPasswordChange) {
                    this.requiresPasswordChange = true;
                    this.userId = result.userId;
                    // Check if this is initial setup (user has not successfully logged in)
                    if (!result.success) {
                        this.isInitialSetup = true;
                        this.password = '';
                    }
                } else {
                    // Successfully logged in without password change requirement, use Identity API
                    await this.signInWithIdentityApi();
                }
            } else {
                let errorDetail = 'Invalid username or password';
                try {
                    const error = await response.json();
                    errorDetail = error.errorMessage || error.detail || error.title || errorDetail;
                } catch {
                    errorDetail = response.statusText || `Error ${response.status}`;
                }
                this.errorMessage = errorDetail;
            }
        } catch (error) {
            console.error('Login error:', error);
            this.errorMessage = 'An error occurred while signing in. Please try again.';
        } finally {
            this.isLoggingIn = false;
        }
    }

    async signInWithIdentityApi() {
        const response = await fetch('/identity/login?useCookies=true', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                email: this.username,
                password: this.password,
            }),
            credentials: 'include',
        });

        if (response.ok) {
            window.location.href = '/';
        } else {
            this.errorMessage = 'Failed to complete sign in. Please try again.';
        }
    }

    async changePassword() {
        if (!this.newPassword || !this.confirmPassword) {
            this.errorMessage = 'Please enter and confirm your new password.';
            return;
        }

        if (this.newPassword !== this.confirmPassword) {
            this.errorMessage = 'Passwords do not match.';
            return;
        }

        if (!this.isInitialSetup && this.newPassword === this.password) {
            this.errorMessage = 'New password must be different from your current password.';
            return;
        }

        this.isLoggingIn = true;
        this.errorMessage = '';

        if (this.isInitialSetup) {
            // Use SetInitialAdminPassword for initial setup
            this._setInitialAdminPassword.userId = this.userId!;
            this._setInitialAdminPassword.password = this.newPassword;
            this._setInitialAdminPassword.confirmedPassword = this.confirmPassword;
            const result = await this._setInitialAdminPassword.execute();
            result
                .onSuccess(async () => {
                    // After setting initial password, sign in with the new credentials
                    this.password = this.newPassword;
                    await this.signInWithIdentityApi();
                })
                .onException((messages) => {
                    this.errorMessage = `Failed to set password: ${messages.join('; ')}`;
                })
                .onValidationFailure((validationResults) => {
                    const errors = validationResults.map(vr => vr.message);
                    this.errorMessage = `Password validation failed: ${errors.join('; ')}`;
                })
                .onUnauthorized(() => {
                    this.errorMessage = 'You are not authorized to set the password.';
                });
        } else {
            // Use ChangePasswordForUser for regular password changes
            this._changePassword.userId = this.userId!;
            this._changePassword.oldPassword = this.password;
            this._changePassword.password = this.newPassword;
            this._changePassword.confirmedPassword = this.confirmPassword;
            const result = await this._changePassword.execute();
            result
                .onSuccess(async () => {
                    this.password = this.newPassword;
                    await this.signInWithIdentityApi();
                })
                .onException((messages) => {
                    this.errorMessage = `Failed to change password: ${messages.join('; ')}`;
                })
                .onValidationFailure((validationResults) => {
                    const errors = validationResults.map(vr => vr.message);
                    this.errorMessage = `Password change validation failed: ${errors.join('; ')}`;
                })
                .onUnauthorized(() => {
                    this.errorMessage = 'You are not authorized to change the password.';
                });
        }

        this.isLoggingIn = false;
    }

    cancelPasswordChange() {
        this.requiresPasswordChange = false;
        this.isInitialSetup = false;
        this.newPassword = '';
        this.confirmPassword = '';
        this.userId = null;
        this.errorMessage = '';

        if (typeof document !== 'undefined') {
            const activeElement = document.activeElement;
            if (activeElement instanceof HTMLElement) {
                activeElement.blur();
            }
        }
    }
}
