// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { injectable } from 'tsyringe';

@injectable()
export class LoginViewModel {
    username: string = '';
    password: string = '';
    isLoggingIn: boolean = false;
    errorMessage: string = '';

    async login() {
        this.isLoggingIn = true;
        this.errorMessage = '';

        try {
            const response = await fetch('/identity/login?useCookies=true', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    email: this.username, // Identity API uses 'email' field but we'll use username
                    password: this.password,
                }),
                credentials: 'include',
            });

            if (response.ok) {
                // Successfully logged in, redirect to home
                window.location.href = '/';
            } else {
                const error = await response.json();
                this.errorMessage = error.detail || 'Invalid username or password';
            }
        } catch (error) {
            console.error('Login error:', error);
            this.errorMessage = 'An error occurred while signing in. Please try again.';
        } finally {
            this.isLoggingIn = false;
        }
    }
}
