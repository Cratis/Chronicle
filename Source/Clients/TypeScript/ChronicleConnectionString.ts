// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as grpc from '@grpc/grpc-js';

/**
 * Authentication mode for Chronicle connection
 */
export enum AuthenticationMode {
    /**
     * Client credentials authentication (username:password)
     */
    ClientCredentials = 'ClientCredentials',

    /**
     * API key authentication
     */
    ApiKey = 'ApiKey',
}

/**
 * Represents a server address with host and port
 */
export interface ChronicleServerAddress {
    /**
     * The host name or IP address
     */
    host: string;

    /**
     * The port number
     */
    port: number;
}

/**
 * Builder for constructing Chronicle connection strings
 */
export class ChronicleConnectionStringBuilder {
    private static readonly DEFAULT_PORT = 35000;
    private static readonly HOST_KEY = 'Host';
    private static readonly PORT_KEY = 'Port';
    private static readonly USERNAME_KEY = 'Username';
    private static readonly PASSWORD_KEY = 'Password';
    private static readonly SCHEME_KEY = 'Scheme';
    private static readonly API_KEY_KEY = 'apiKey';
    private static readonly DISABLE_TLS_KEY = 'disableTls';
    private static readonly CERTIFICATE_PATH_KEY = 'certificatePath';
    private static readonly CERTIFICATE_PASSWORD_KEY = 'certificatePassword';

    private properties: Map<string, string> = new Map();

    /**
     * Creates a new connection string builder
     * @param connectionString Optional connection string to parse
     */
    constructor(connectionString?: string) {
        if (connectionString) {
            this.parseConnectionString(connectionString);
        }
    }

    /**
     * Gets or sets the host
     */
    get host(): string {
        return this.properties.get(ChronicleConnectionStringBuilder.HOST_KEY) || 'localhost';
    }

    set host(value: string) {
        this.properties.set(ChronicleConnectionStringBuilder.HOST_KEY, value);
    }

    /**
     * Gets or sets the port
     */
    get port(): number {
        const portStr = this.properties.get(ChronicleConnectionStringBuilder.PORT_KEY);
        return portStr ? parseInt(portStr, 10) : ChronicleConnectionStringBuilder.DEFAULT_PORT;
    }

    set port(value: number) {
        this.properties.set(ChronicleConnectionStringBuilder.PORT_KEY, value.toString());
    }

    /**
     * Gets or sets the username for authentication
     */
    get username(): string | undefined {
        return this.properties.get(ChronicleConnectionStringBuilder.USERNAME_KEY);
    }

    set username(value: string | undefined) {
        if (value) {
            this.properties.set(ChronicleConnectionStringBuilder.USERNAME_KEY, value);
        } else {
            this.properties.delete(ChronicleConnectionStringBuilder.USERNAME_KEY);
        }
    }

    /**
     * Gets or sets the password for authentication
     */
    get password(): string | undefined {
        return this.properties.get(ChronicleConnectionStringBuilder.PASSWORD_KEY);
    }

    set password(value: string | undefined) {
        if (value) {
            this.properties.set(ChronicleConnectionStringBuilder.PASSWORD_KEY, value);
        } else {
            this.properties.delete(ChronicleConnectionStringBuilder.PASSWORD_KEY);
        }
    }

    /**
     * Gets or sets the scheme
     */
    get scheme(): string {
        return this.properties.get(ChronicleConnectionStringBuilder.SCHEME_KEY) || 'chronicle';
    }

    set scheme(value: string) {
        this.properties.set(ChronicleConnectionStringBuilder.SCHEME_KEY, value);
    }

    /**
     * Gets the authentication mode based on configured credentials
     */
    get authenticationMode(): AuthenticationMode {
        const hasClientCredentials = !!this.username && !!this.password;
        const hasApiKey = !!this.apiKey;

        if (hasClientCredentials && hasApiKey) {
            throw new Error('Cannot specify both client credentials and API key authentication');
        }

        if (hasClientCredentials) {
            return AuthenticationMode.ClientCredentials;
        }

        if (hasApiKey) {
            return AuthenticationMode.ApiKey;
        }

        throw new Error('No authentication method specified. Please provide either client credentials or API key');
    }

    /**
     * Gets or sets the API key for authentication
     */
    get apiKey(): string | undefined {
        return this.properties.get(ChronicleConnectionStringBuilder.API_KEY_KEY);
    }

    set apiKey(value: string | undefined) {
        if (value) {
            this.properties.set(ChronicleConnectionStringBuilder.API_KEY_KEY, value);
        } else {
            this.properties.delete(ChronicleConnectionStringBuilder.API_KEY_KEY);
        }
    }

    /**
     * Gets or sets whether TLS is disabled
     */
    get disableTls(): boolean {
        const value = this.properties.get(ChronicleConnectionStringBuilder.DISABLE_TLS_KEY);
        return value === 'true';
    }

    set disableTls(value: boolean) {
        this.properties.set(ChronicleConnectionStringBuilder.DISABLE_TLS_KEY, value.toString());
    }

    /**
     * Gets or sets the certificate path
     */
    get certificatePath(): string | undefined {
        return this.properties.get(ChronicleConnectionStringBuilder.CERTIFICATE_PATH_KEY);
    }

    set certificatePath(value: string | undefined) {
        if (value) {
            this.properties.set(ChronicleConnectionStringBuilder.CERTIFICATE_PATH_KEY, value);
        } else {
            this.properties.delete(ChronicleConnectionStringBuilder.CERTIFICATE_PATH_KEY);
        }
    }

    /**
     * Gets or sets the certificate password
     */
    get certificatePassword(): string | undefined {
        return this.properties.get(ChronicleConnectionStringBuilder.CERTIFICATE_PASSWORD_KEY);
    }

    set certificatePassword(value: string | undefined) {
        if (value) {
            this.properties.set(ChronicleConnectionStringBuilder.CERTIFICATE_PASSWORD_KEY, value);
        } else {
            this.properties.delete(ChronicleConnectionStringBuilder.CERTIFICATE_PASSWORD_KEY);
        }
    }

    /**
     * Builds the connection string
     */
    build(): string {
        let url = `${this.scheme}://`;

        if (this.username) {
            url += this.username;
            if (this.password) {
                url += `:${this.password}`;
            }
            url += '@';
        }

        url += this.host;
        url += `:${this.port}`;

        const queryParams: string[] = [];

        if (this.apiKey) {
            queryParams.push(`apiKey=${encodeURIComponent(this.apiKey)}`);
        }

        if (this.disableTls) {
            queryParams.push('disableTls=true');
        }

        if (this.certificatePath) {
            queryParams.push(`certificatePath=${encodeURIComponent(this.certificatePath)}`);
        }

        if (this.certificatePassword) {
            queryParams.push(`certificatePassword=${encodeURIComponent(this.certificatePassword)}`);
        }

        // Add any other custom query parameters
        for (const [key, value] of this.properties) {
            if (
                key !== ChronicleConnectionStringBuilder.HOST_KEY &&
                key !== ChronicleConnectionStringBuilder.PORT_KEY &&
                key !== ChronicleConnectionStringBuilder.USERNAME_KEY &&
                key !== ChronicleConnectionStringBuilder.PASSWORD_KEY &&
                key !== ChronicleConnectionStringBuilder.SCHEME_KEY &&
                key !== ChronicleConnectionStringBuilder.API_KEY_KEY &&
                key !== ChronicleConnectionStringBuilder.DISABLE_TLS_KEY &&
                key !== ChronicleConnectionStringBuilder.CERTIFICATE_PATH_KEY &&
                key !== ChronicleConnectionStringBuilder.CERTIFICATE_PASSWORD_KEY
            ) {
                queryParams.push(`${encodeURIComponent(key)}=${encodeURIComponent(value)}`);
            }
        }

        if (queryParams.length > 0) {
            url += `?${queryParams.join('&')}`;
        }

        return url;
    }

    private parseConnectionString(connectionString: string): void {
        if (!connectionString) {
            return;
        }

        // Check if it's a Chronicle URL format
        if (
            connectionString.startsWith('chronicle://') ||
            connectionString.startsWith('chronicle+srv://')
        ) {
            this.parseUrl(connectionString);
        }
    }

    private parseUrl(url: string): void {
        try {
            const parsed = new URL(url);

            // Extract scheme
            this.scheme = parsed.protocol.replace(':', '');

            // Extract host
            this.host = parsed.hostname;

            // Extract port
            if (parsed.port) {
                this.port = parseInt(parsed.port, 10);
            }

            // Extract username and password from userInfo
            if (parsed.username) {
                this.username = decodeURIComponent(parsed.username);
                if (parsed.password) {
                    this.password = decodeURIComponent(parsed.password);
                }
            }

            // Parse query string parameters
            if (parsed.search) {
                const searchParams = new URLSearchParams(parsed.search);
                for (const [key, value] of searchParams) {
                    this.properties.set(key, decodeURIComponent(value));
                }
            }
        } catch (error) {
            throw new Error(`Invalid Chronicle connection string: ${error}`);
        }
    }
}

/**
 * Represents a Chronicle connection string
 *
 * Supported formats:
 * - chronicle://<host>[:<port>]/?<options>
 * - chronicle://username:password@<host>[:<port>]/?<options>
 * - chronicle+srv://<host>[:<port>]/?<options>
 * - chronicle://<host>[:<port>],<host>[:<port>],<host>[:<port>]/?<options>
 */
export class ChronicleConnectionString {
    /**
     * The default client ID for development purposes
     */
    static readonly DEVELOPMENT_CLIENT = 'chronicle-dev-client';

    /**
     * The default client secret for development purposes
     */
    static readonly DEVELOPMENT_CLIENT_SECRET = 'chronicle-dev-secret';

    /**
     * The default connection string pointing to localhost
     */
    static readonly Default = new ChronicleConnectionString('chronicle://localhost:35000');

    /**
     * The development connection string pointing to localhost configured with the default dev credentials
     */
    static readonly Development = new ChronicleConnectionString(
        `chronicle://${ChronicleConnectionString.DEVELOPMENT_CLIENT}:${ChronicleConnectionString.DEVELOPMENT_CLIENT_SECRET}@localhost:35000`
    );

    private readonly builder: ChronicleConnectionStringBuilder;
    private readonly _serverAddress: ChronicleServerAddress;

    /**
     * Creates a new Chronicle connection string
     * @param connectionString The connection string to parse
     */
    constructor(connectionString: string) {
        this.builder = new ChronicleConnectionStringBuilder(connectionString);
        this._serverAddress = {
            host: this.builder.host,
            port: this.builder.port,
        };
    }

    /**
     * Gets the server address
     */
    get serverAddress(): ChronicleServerAddress {
        return this._serverAddress;
    }

    /**
     * Gets the username for authentication (client id for client_credentials flow)
     */
    get username(): string | undefined {
        return this.builder.username;
    }

    /**
     * Gets the password for authentication (client secret for client_credentials flow)
     */
    get password(): string | undefined {
        return this.builder.password;
    }

    /**
     * Gets the authentication mode
     */
    get authenticationMode(): AuthenticationMode {
        return this.builder.authenticationMode;
    }

    /**
     * Gets the API key for ApiKey authentication
     */
    get apiKey(): string | undefined {
        return this.builder.apiKey;
    }

    /**
     * Gets whether TLS is disabled
     */
    get disableTls(): boolean {
        return this.builder.disableTls;
    }

    /**
     * Gets the certificate path
     */
    get certificatePath(): string | undefined {
        return this.builder.certificatePath;
    }

    /**
     * Gets the certificate password
     */
    get certificatePassword(): string | undefined {
        return this.builder.certificatePassword;
    }

    /**
     * Creates a new connection string with the specified username and password (Client Credentials)
     * @param username The username to set
     * @param password The password to set
     * @returns A new connection string with the credentials set
     */
    withCredentials(username: string, password: string): ChronicleConnectionString {
        const newBuilder = new ChronicleConnectionStringBuilder(this.toString());
        newBuilder.username = username;
        newBuilder.password = password;
        return new ChronicleConnectionString(newBuilder.build());
    }

    /**
     * Creates a new connection string with API key authentication
     * @param apiKey The API key to use
     * @returns A new connection string with API key authentication configured
     */
    withApiKey(apiKey: string): ChronicleConnectionString {
        const newBuilder = new ChronicleConnectionStringBuilder(this.toString());
        newBuilder.apiKey = apiKey;
        return new ChronicleConnectionString(newBuilder.build());
    }

    /**
     * Creates gRPC channel credentials based on the connection string configuration
     * @returns gRPC channel credentials
     */
    createCredentials(): grpc.ChannelCredentials {
        if (this.disableTls) {
            return grpc.credentials.createInsecure();
        }

        // TODO: Add support for certificate-based TLS
        // For now, use createSsl() with no parameters for default TLS
        return grpc.credentials.createSsl();
    }

    /**
     * Creates gRPC call credentials for authentication
     * @returns gRPC call credentials or undefined if no authentication is configured
     */
    createCallCredentials(): grpc.CallCredentials | undefined {
        try {
            const mode = this.authenticationMode;

            if (mode === AuthenticationMode.ClientCredentials && this.username && this.password) {
                // For client credentials, we add the credentials as metadata
                return grpc.credentials.createFromMetadataGenerator((params, callback) => {
                    const metadata = new grpc.Metadata();
                    metadata.add('client-id', this.username!);
                    metadata.add('client-secret', this.password!);
                    callback(null, metadata);
                });
            }

            if (mode === AuthenticationMode.ApiKey && this.apiKey) {
                // For API key, we add it as a bearer token or api key header
                return grpc.credentials.createFromMetadataGenerator((params, callback) => {
                    const metadata = new grpc.Metadata();
                    metadata.add('api-key', this.apiKey!);
                    callback(null, metadata);
                });
            }
        } catch {
            // No authentication configured
            return undefined;
        }

        return undefined;
    }

    /**
     * Returns the connection string representation
     */
    toString(): string {
        return this.builder.build();
    }
}
