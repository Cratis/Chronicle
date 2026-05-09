// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as grpc from '@grpc/grpc-js';
import { createClient } from 'nice-grpc';

// Import service definitions from generated files
import { EventStoresDefinition, NamespacesDefinition } from './generated/cratis_chronicle_contracts';
import { RecommendationsDefinition } from './generated/recommendations';
import { IdentitiesDefinition } from './generated/identities';
import { EventSequencesDefinition } from './generated/eventsequences';
import { EventTypesDefinition } from './generated/events';
import { ConstraintsDefinition } from './generated/events_constraints';
import { ObserversDefinition, FailedPartitionsDefinition } from './generated/observation';
import { ReactorsDefinition } from './generated/observation_reactors';
import { ReducersDefinition } from './generated/observation_reducers';
import { ProjectionsDefinition } from './generated/projections';
import { ReadModelsDefinition } from './generated/readmodels';
import { JobsDefinition } from './generated/jobs';
import { EventSeedingDefinition } from './generated/seeding';
import { ServerDefinition } from './generated/host';
import { ConnectionServiceDefinition } from './generated/clients';

// Import client types
import type {
    EventStoresClient,
    NamespacesClient,
    RecommendationsClient,
    IdentitiesClient,
    EventSequencesClient,
    EventTypesClient,
    ConstraintsClient,
    ObserversClient,
    FailedPartitionsClient,
    ReactorsClient,
    ReducersClient,
    ProjectionsClient,
    ReadModelsClient,
    JobsClient,
    EventSeedingClient,
    ServerClient,
    ConnectionServiceClient,
} from './generated/index';

import type { ChronicleServices } from './ChronicleServices';
import { ChronicleConnectionString, AuthenticationMode } from './ChronicleConnectionString';
import { ITokenProvider, OAuthTokenProvider, NoOpTokenProvider } from './TokenProvider';

/**
 * Configuration options for Chronicle connection
 */
export interface ChronicleConnectionOptions {
    /**
     * The connection string (e.g., 'chronicle://localhost:35000' or 'chronicle://user:pass@localhost:35000')
     * Can also be a ChronicleConnectionString instance
     */
    connectionString?: string | ChronicleConnectionString;

    /**
     * The host and port of the Chronicle server (e.g., 'localhost:35000')
     * This is used if connectionString is not provided
     */
    serverAddress?: string;

    /**
     * Optional gRPC credentials (defaults to credentials based on connection string)
     */
    credentials?: grpc.ChannelCredentials;

    /**
     * Optional connection timeout in milliseconds (defaults to 10000)
     */
    connectTimeout?: number;

    /**
     * Optional maximum receive message size in bytes
     */
    maxReceiveMessageSize?: number;

    /**
     * Optional maximum send message size in bytes
     */
    maxSendMessageSize?: number;

    /**
     * Optional correlation ID for tracking requests
     */
    correlationId?: string;

    /**
     * Optional authentication authority URL. If not set, uses the Chronicle server itself as the authority
     */
    authority?: string;

    /**
     * Optional management port for authentication endpoint (defaults to 8080)
     */
    managementPort?: number;
}

/**
 * Manages the connection to Chronicle Kernel and provides access to all gRPC services
 */
export class ChronicleConnection implements ChronicleServices {
    private readonly channel: grpc.Channel;
    private readonly services: ChronicleServices;
    private readonly _connectionString: ChronicleConnectionString;
    private readonly tokenProvider: ITokenProvider;
    private _isConnected = false;

    /**
     * Event stores service
     */
    get eventStores(): EventStoresClient {
        return this.services.eventStores;
    }

    /**
     * Namespaces service
     */
    get namespaces(): NamespacesClient {
        return this.services.namespaces;
    }

    /**
     * Recommendations service
     */
    get recommendations(): RecommendationsClient {
        return this.services.recommendations;
    }

    /**
     * Identities service
     */
    get identities(): IdentitiesClient {
        return this.services.identities;
    }

    /**
     * Event sequences service
     */
    get eventSequences(): EventSequencesClient {
        return this.services.eventSequences;
    }

    /**
     * Event types service
     */
    get eventTypes(): EventTypesClient {
        return this.services.eventTypes;
    }

    /**
     * Constraints service
     */
    get constraints(): ConstraintsClient {
        return this.services.constraints;
    }

    /**
     * Observers service
     */
    get observers(): ObserversClient {
        return this.services.observers;
    }

    /**
     * Failed partitions service
     */
    get failedPartitions(): FailedPartitionsClient {
        return this.services.failedPartitions;
    }

    /**
     * Reactors service
     */
    get reactors(): ReactorsClient {
        return this.services.reactors;
    }

    /**
     * Reducers service
     */
    get reducers(): ReducersClient {
        return this.services.reducers;
    }

    /**
     * Projections service
     */
    get projections(): ProjectionsClient {
        return this.services.projections;
    }

    /**
     * Read models service
     */
    get readModels(): ReadModelsClient {
        return this.services.readModels;
    }

    /**
     * Jobs service
     */
    get jobs(): JobsClient {
        return this.services.jobs;
    }

    /**
     * Event seeding service
     */
    get eventSeeding(): EventSeedingClient {
        return this.services.eventSeeding;
    }

    /**
     * Server service
     */
    get server(): ServerClient {
        return this.services.server;
    }

    /**
     * Connection service for managing the connection
     */
    private connectionService: ConnectionServiceClient;

    /**
     * Gets the connection string used for this connection
     */
    get connectionString(): ChronicleConnectionString {
        return this._connectionString;
    }

    /**
     * Creates a new Chronicle connection
     * @param options Connection configuration options
     */
    constructor(options: ChronicleConnectionOptions) {
        // Parse connection string or create from serverAddress
        if (options.connectionString) {
            this._connectionString =
                typeof options.connectionString === 'string'
                    ? new ChronicleConnectionString(options.connectionString)
                    : options.connectionString;
        } else if (options.serverAddress) {
            this._connectionString = new ChronicleConnectionString(
                `chronicle://${options.serverAddress}`
            );
        } else {
            this._connectionString = ChronicleConnectionString.Default;
        }

        // Create server address string
        const serverAddress = `${this._connectionString.serverAddress.host}:${this._connectionString.serverAddress.port}`;

        // Create token provider for authentication
        this.tokenProvider = this.createTokenProvider(options);

        // Create channel options
        const channelOptions: grpc.ChannelOptions = {};

        if (options.maxReceiveMessageSize) {
            channelOptions['grpc.max_receive_message_length'] = options.maxReceiveMessageSize;
        }

        if (options.maxSendMessageSize) {
            channelOptions['grpc.max_send_message_length'] = options.maxSendMessageSize;
        }

        // Create gRPC channel credentials
        let channelCredentials = options.credentials;
        if (!channelCredentials) {
            channelCredentials = this._connectionString.createCredentials();

            if (!this._connectionString.disableTls) {
                const callCredentials = this.createAuthCallCredentials();
                if (callCredentials) {
                    channelCredentials = grpc.credentials.combineChannelCredentials(
                        channelCredentials,
                        callCredentials
                    );
                }
            }
        }

        // Create the gRPC channel
        this.channel = new grpc.Channel(serverAddress, channelCredentials, channelOptions);

        // Create services using nice-grpc's createClient
        this.connectionService = createClient(ConnectionServiceDefinition, this.channel);
        this.services = this.createServices();
    }

    private createServices(): ChronicleServices {
        return {
            eventStores: createClient(EventStoresDefinition, this.channel),
            namespaces: createClient(NamespacesDefinition, this.channel),
            recommendations: createClient(RecommendationsDefinition, this.channel),
            identities: createClient(IdentitiesDefinition, this.channel),
            eventSequences: createClient(EventSequencesDefinition, this.channel),
            eventTypes: createClient(EventTypesDefinition, this.channel),
            constraints: createClient(ConstraintsDefinition, this.channel),
            observers: createClient(ObserversDefinition, this.channel),
            failedPartitions: createClient(FailedPartitionsDefinition, this.channel),
            reactors: createClient(ReactorsDefinition, this.channel),
            reducers: createClient(ReducersDefinition, this.channel),
            projections: createClient(ProjectionsDefinition, this.channel),
            readModels: createClient(ReadModelsDefinition, this.channel),
            jobs: createClient(JobsDefinition, this.channel),
            eventSeeding: createClient(EventSeedingDefinition, this.channel),
            server: createClient(ServerDefinition, this.channel),
        };
    }

    /**
     * Creates a token provider based on connection configuration
     */
    private createTokenProvider(options: ChronicleConnectionOptions): ITokenProvider {
        const hasUsername = !!this._connectionString.username;
        const hasPassword = !!this._connectionString.password;
        const hasApiKey = !!this._connectionString.apiKey;

        if (hasApiKey) {
            // API key authentication is handled directly through call metadata.
            return new NoOpTokenProvider();
        }

        if (hasUsername !== hasPassword) {
            // Incomplete client credentials cannot produce a valid OAuth token.
            return new NoOpTokenProvider();
        }

        if (hasUsername && hasPassword) {
            return this.createOAuthTokenProvider(
                options,
                this._connectionString.username!,
                this._connectionString.password!
            );
        }

        // Development support: no app credentials provided, so use default dev credentials.
        return this.createOAuthTokenProvider(
            options,
            ChronicleConnectionString.DEVELOPMENT_CLIENT,
            ChronicleConnectionString.DEVELOPMENT_CLIENT_SECRET
        );
    }

    private createOAuthTokenProvider(
        options: ChronicleConnectionOptions,
        username: string,
        password: string
    ): ITokenProvider {
        const managementPort = options.managementPort || 8080;
        let authorityHost: string;
        let authorityPort: number;

        if (options.authority) {
            const authorityUrl = new URL(options.authority);
            authorityHost = authorityUrl.hostname;
            authorityPort = authorityUrl.port ? parseInt(authorityUrl.port, 10) : managementPort;
        } else {
            authorityHost = this._connectionString.serverAddress.host;
            authorityPort = managementPort;
        }

        const scheme = this._connectionString.disableTls ? 'http' : 'https';
        const tokenEndpoint = `${scheme}://${authorityHost}:${authorityPort}/connect/token`;

        return new OAuthTokenProvider(tokenEndpoint, username, password);
    }

    /**
     * Creates call credentials with bearer token from token provider
     */
    private createAuthCallCredentials(): grpc.CallCredentials | undefined {
        // Insecure channels cannot compose with call credentials — skip when TLS is disabled.
        if (this._connectionString.disableTls) {
            return undefined;
        }
        return grpc.credentials.createFromMetadataGenerator(async (params, callback) => {
            try {
                const token = await this.tokenProvider.getAccessToken();
                const metadata = new grpc.Metadata();

                if (token) {
                    metadata.add('authorization', `Bearer ${token}`);
                } else {
                    // Check for API key authentication
                    try {
                        const authMode = this._connectionString.authenticationMode;
                        if (authMode === AuthenticationMode.ApiKey && this._connectionString.apiKey) {
                            metadata.add('api-key', this._connectionString.apiKey);
                        }
                    } catch {
                        // No API key configured
                    }
                }

                callback(null, metadata);
            } catch (error) {
                callback(error as Error, new grpc.Metadata());
            }
        });
    }

    /**
     * Gets whether the connection is established
     */
    get isConnected(): boolean {
        return this._isConnected;
    }

    /**
     * Connects to the Chronicle Kernel
     * @returns Promise that resolves when connected
     */
    async connect(): Promise<void> {
        if (this._isConnected) {
            return;
        }

        return new Promise<void>((resolve, reject) => {
            const deadline = new Date();
            deadline.setSeconds(deadline.getSeconds() + 10);

            this.channel.watchConnectivityState(
                this.channel.getConnectivityState(true),
                deadline,
                (error) => {
                    if (error) {
                        this._isConnected = false;
                        reject(error);
                    } else {
                        const state = this.channel.getConnectivityState(false);
                        if (state === grpc.connectivityState.READY) {
                            this._isConnected = true;
                            resolve();
                        } else {
                            this._isConnected = false;
                            reject(new Error(`Connection failed with state: ${state}`));
                        }
                    }
                }
            );
        });
    }

    /**
     * Disconnects from the Chronicle Kernel
     */
    disconnect(): void {
        this._isConnected = false;
        this.channel.close();
    }

    /**
     * Disposes the connection and cleans up resources
     */
    dispose(): void {
        this.disconnect();
    }
}

