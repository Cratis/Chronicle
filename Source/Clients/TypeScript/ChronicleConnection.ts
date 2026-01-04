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

/**
 * Configuration options for Chronicle connection
 */
export interface ChronicleConnectionOptions {
    /**
     * The host and port of the Chronicle server (e.g., 'localhost:5000')
     */
    serverAddress: string;

    /**
     * Optional gRPC credentials (defaults to insecure credentials)
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
     * Creates a new Chronicle connection
     * @param options Connection configuration options
     */
    constructor(options: ChronicleConnectionOptions) {
        const credentials = options.credentials ?? grpc.credentials.createInsecure();
        
        const channelOptions: grpc.ChannelOptions = {};
        
        if (options.maxReceiveMessageSize) {
            channelOptions['grpc.max_receive_message_length'] = options.maxReceiveMessageSize;
        }
        
        if (options.maxSendMessageSize) {
            channelOptions['grpc.max_send_message_length'] = options.maxSendMessageSize;
        }

        this.channel = new grpc.Channel(options.serverAddress, credentials, channelOptions);

        // Initialize all service clients
        this.connectionService = new ConnectionServiceClient(options.serverAddress, credentials);
        
        this.services = {
            eventStores: new EventStoresClient(options.serverAddress, credentials),
            namespaces: new NamespacesClient(options.serverAddress, credentials),
            recommendations: new RecommendationsClient(options.serverAddress, credentials),
            identities: new IdentitiesClient(options.serverAddress, credentials),
            eventSequences: new EventSequencesClient(options.serverAddress, credentials),
            eventTypes: new EventTypesClient(options.serverAddress, credentials),
            constraints: new ConstraintsClient(options.serverAddress, credentials),
            observers: new ObserversClient(options.serverAddress, credentials),
            failedPartitions: new FailedPartitionsClient(options.serverAddress, credentials),
            reactors: new ReactorsClient(options.serverAddress, credentials),
            reducers: new ReducersClient(options.serverAddress, credentials),
            projections: new ProjectionsClient(options.serverAddress, credentials),
            readModels: new ReadModelsClient(options.serverAddress, credentials),
            jobs: new JobsClient(options.serverAddress, credentials),
            eventSeeding: new EventSeedingClient(options.serverAddress, credentials),
            server: new ServerClient(options.serverAddress, credentials),
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
