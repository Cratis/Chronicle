// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as https from 'https';
import * as http from 'http';

/**
 * Interface for providing authentication tokens
 */
export interface ITokenProvider {
    /**
     * Gets the current access token
     * @returns Promise resolving to the access token or undefined if not available
     */
    getAccessToken(): Promise<string | undefined>;

    /**
     * Refreshes the access token by clearing cached tokens and obtaining a new one
     * @returns Promise resolving to the new access token or undefined if not available
     */
    refresh(): Promise<string | undefined>;
}

/**
 * No-op token provider for when authentication is not required
 */
export class NoOpTokenProvider implements ITokenProvider {
    async getAccessToken(): Promise<string | undefined> {
        return undefined;
    }

    async refresh(): Promise<string | undefined> {
        return undefined;
    }
}

/**
 * OAuth token response from the token endpoint
 */
interface OAuthTokenResponse {
    access_token: string;
    expires_in: number;
    token_type: string;
}

/**
 * OAuth token provider using client credentials flow
 */
export class OAuthTokenProvider implements ITokenProvider {
    private accessToken?: string;
    private tokenExpiry: Date = new Date(0);
    private refreshPromise?: Promise<string | undefined>;

    /**
     * Creates a new OAuth token provider
     * @param tokenEndpoint The token endpoint URL
     * @param clientId The OAuth client ID
     * @param clientSecret The OAuth client secret
     */
    constructor(
        private readonly tokenEndpoint: string,
        private readonly clientId: string,
        private readonly clientSecret: string
    ) {}

    /**
     * Gets the current access token, fetching a new one if needed
     */
    async getAccessToken(): Promise<string | undefined> {
        if (this.accessToken && new Date() < this.tokenExpiry) {
            return this.accessToken;
        }

        // If we're already refreshing, wait for that promise
        if (this.refreshPromise) {
            return this.refreshPromise;
        }

        this.refreshPromise = this.fetchAccessToken();
        try {
            return await this.refreshPromise;
        } finally {
            this.refreshPromise = undefined;
        }
    }

    /**
     * Refreshes the access token by clearing the cache and fetching a new one
     */
    async refresh(): Promise<string | undefined> {
        this.accessToken = undefined;
        this.tokenExpiry = new Date(0);
        return this.getAccessToken();
    }

    /**
     * Fetches a new access token from the token endpoint
     */
    private async fetchAccessToken(): Promise<string | undefined> {
        const params = new URLSearchParams();
        params.append('grant_type', 'client_credentials');
        params.append('client_id', this.clientId);
        params.append('client_secret', this.clientSecret);

        const body = params.toString();

        return new Promise((resolve, reject) => {
            const url = new URL(this.tokenEndpoint);
            const isHttps = url.protocol === 'https:';
            const httpModule = isHttps ? https : http;

            const options = {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'Content-Length': Buffer.byteLength(body),
                },
                // Accept self-signed certificates in development
                rejectUnauthorized: false,
            };

            const req = httpModule.request(url, options, (res) => {
                let data = '';

                res.on('data', (chunk) => {
                    data += chunk;
                });

                res.on('end', () => {
                    if (res.statusCode === 200) {
                        try {
                            const tokenResponse: OAuthTokenResponse = JSON.parse(data);
                            this.accessToken = tokenResponse.access_token;

                            // Set expiry with 60 second buffer
                            const expiresInSeconds = tokenResponse.expires_in || 3600;
                            this.tokenExpiry = new Date(
                                Date.now() + (expiresInSeconds - 60) * 1000
                            );

                            resolve(this.accessToken);
                        } catch (error) {
                            reject(
                                new Error(
                                    `Failed to parse token response: ${error instanceof Error ? error.message : String(error)}`
                                )
                            );
                        }
                    } else {
                        reject(
                            new Error(
                                `Token request failed with status ${res.statusCode}: ${data}`
                            )
                        );
                    }
                });
            });

            req.on('error', (error) => {
                reject(new Error(`Token request failed: ${error.message}`));
            });

            req.write(body);
            req.end();
        });
    }
}
