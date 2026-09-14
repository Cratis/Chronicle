// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';
import { Guid } from '@cratis/fundamentals';
import { ChangeUserPassword } from './ChangeUserPassword';
import { GetStatus } from './GetStatus';
import { SetInitialAdminPassword } from './SetInitialAdminPassword';
import { absolutePath } from '../../Utils/basePath';
import { clearAntiforgeryToken, refreshAntiforgeryToken } from './antiforgery';

@injectable()
export class LoginViewModel {
    username: string = '';
    password: string = '';
    newPassword: string = '';
    confirmPassword: string = '';
    isLoggingIn: boolean = false;
    isLoading: boolean = true;
    errorMessage: string = '';
    requiresPasswordChange: boolean = false;
    isInitialSetup: boolean = false;
    userId: Guid | null = null;

    constructor(
        readonly _changePassword: ChangeUserPassword,
        readonly _setInitialAdminPassword: SetInitialAdminPassword,
        readonly _getStatus: GetStatus) {
    }

    async checkInitialSetup() {
        this.isLoading = true;
        try {
            const result = await this._getStatus.perform();
            if (result.data?.isRequired) {
                this.isInitialSetup = true;
                this.requiresPasswordChange = true;
                this.userId = result.data.adminUserId ?? null;
                this.username = result.data.adminUsername || 'admin';
            }
        } catch (error) {
            console.error('Failed to check initial setup status:', error);
        } finally {
            this.isLoading = false;
        }
    }

    async login() {
        this.isLoggingIn = true;
        this.errorMessage = '';

        try {
            const response = await fetch(absolutePath('/api/security/login'), {
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

                if (!result.success) {
                    this.errorMessage = result.errorMessage || 'Invalid username or password';
                } else if (result.requiresPasswordChange) {
                    // Password changes are authenticated commands. Establish the cookie and request token first.
                    if (await this.signInWithIdentityApi(false)) {
                        this.requiresPasswordChange = true;
                        this.userId = result.userId;
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

    async signInWithIdentityApi(redirect: boolean = true): Promise<boolean> {
        clearAntiforgeryToken();
        const response = await fetch(absolutePath('/identity/login?useCookies=true'), {
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
            await refreshAntiforgeryToken();
            if (redirect) window.location.href = absolutePath('/');
            return true;
        }
        this.errorMessage = 'Failed to complete sign in. Please try again.';
        return false;
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

        try {
            const command = this.isInitialSetup ? this._setInitialAdminPassword : this._changePassword;
            command.userId = this.userId!;
            command.password = this.newPassword;
            command.confirmedPassword = this.confirmPassword;
            if (!this.isInitialSetup) this._changePassword.oldPassword = this.password;

            const result = await command.execute();
            if (result.isSuccess) {
                this.password = this.newPassword;
                await this.signInWithIdentityApi();
            } else {
                result
                    .onException(messages => {
                        this.errorMessage = `Failed to change password: ${messages.join('; ')}`;
                    })
                    .onValidationFailure(validationResults => {
                        this.errorMessage = `Password validation failed: ${validationResults.map(vr => vr.message).join('; ')}`;
                    })
                    .onUnauthorized(() => {
                        this.errorMessage = 'You are not authorized to change the password.';
                    });
            }
        } catch {
            this.errorMessage = 'Unable to complete the password change. Please try signing in again before retrying.';
        } finally {
            this.isLoggingIn = false;
        }
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
