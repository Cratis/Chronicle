// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as grpc from '@grpc/grpc-js';
import { EventStoresClient } from './generated/cratis_chronicle_contracts';
import { NamespacesClient } from './generated/cratis_chronicle_contracts';
import { RecommendationsClient } from './generated/recommendations';
import { IdentitiesClient } from './generated/identities';
import { EventSequencesClient } from './generated/eventsequences';
import { EventTypesClient } from './generated/events';
import { ConstraintsClient } from './generated/events_constraints';
import { ObserversClient } from './generated/observation';
import { FailedPartitionsClient } from './generated/observation';
import { ReactorsClient } from './generated/observation_reactors';
import { ReducersClient } from './generated/observation_reducers';
import { ProjectionsClient } from './generated/projections';
import { ReadModelsClient } from './generated/readmodels';
import { JobsClient } from './generated/jobs';
import { EventSeedingClient } from './generated/seeding';
import { ServerClient } from './generated/host';
import { ConnectionServiceClient } from './generated/clients';
import type { ChronicleServices } from './ChronicleServices';
import { ChronicleConnectionString } from './ChronicleConnectionString';

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
     * The host and port of the Chronicle server (e.g., 'localhost:5000')
     * This is used if connectionString is not provided
     * @deprecated Use connectionString instead
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
}

/**
 * Manages the connection to Chronicle Kernel and provides access to all gRPC services
 */
export class ChronicleConnection implements ChronicleServices {
    private readonly channel: grpc.Channel;
    private readonly services: ChronicleServices;
    private readonly _connectionString: ChronicleConnectionString;
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

        // Create credentials
        let channelCredentials = options.credentials;
        if (!channelCredentials) {
            channelCredentials = this._connectionString.createCredentials();
            const callCredentials = this._connectionString.createCallCredentials();
            if (callCredentials) {
                channelCredentials = grpc.credentials.combineChannelCredentials(
                    channelCredentials,
                    callCredentials
                );
            }
        }

        const channelOptions: grpc.ChannelOptions = {};

        if (options.maxReceiveMessageSize) {
            channelOptions['grpc.max_receive_message_length'] = options.maxReceiveMessageSize;
        }

        if (options.maxSendMessageSize) {
            channelOptions['grpc.max_send_message_length'] = options.maxSendMessageSize;
        }

        this.channel = new grpc.Channel(serverAddress, channelCredentials, channelOptions);

        // Initialize all service clients
        this.connectionService = new ConnectionServiceClient(serverAddress, channelCredentials);

        this.services = {
            eventStores: new EventStoresClient(serverAddress, channelCredentials),
            namespaces: new NamespacesClient(serverAddress, channelCredentials),
            recommendations: new RecommendationsClient(serverAddress, channelCredentials),
            identities: new IdentitiesClient(serverAddress, channelCredentials),
            eventSequences: new EventSequencesClient(serverAddress, channelCredentials),
            eventTypes: new EventTypesClient(serverAddress, channelCredentials),
            constraints: new ConstraintsClient(serverAddress, channelCredentials),
            observers: new ObserversClient(serverAddress, channelCredentials),
            failedPartitions: new FailedPartitionsClient(serverAddress, channelCredentials),
            reactors: new ReactorsClient(serverAddress, channelCredentials),
            reducers: new ReducersClient(serverAddress, channelCredentials),
            projections: new ProjectionsClient(serverAddress, channelCredentials),
            readModels: new ReadModelsClient(serverAddress, channelCredentials),
            jobs: new JobsClient(serverAddress, channelCredentials),
            eventSeeding: new EventSeedingClient(serverAddress, channelCredentials),
            server: new ServerClient(serverAddress, channelCredentials),
        };
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

