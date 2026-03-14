// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { EventStoresClient } from './generated/cratis_chronicle_contracts';
import type { NamespacesClient } from './generated/cratis_chronicle_contracts';
import type { RecommendationsClient } from './generated/recommendations';
import type { IdentitiesClient } from './generated/identities';
import type { EventSequencesClient } from './generated/eventsequences';
import type { EventTypesClient } from './generated/events';
import type { ConstraintsClient } from './generated/events_constraints';
import type { ObserversClient } from './generated/observation';
import type { FailedPartitionsClient } from './generated/observation';
import type { ReactorsClient } from './generated/observation_reactors';
import type { ReducersClient } from './generated/observation_reducers';
import type { ProjectionsClient } from './generated/projections';
import type { ReadModelsClient } from './generated/readmodels';
import type { JobsClient } from './generated/jobs';
import type { EventSeedingClient } from './generated/seeding';
import type { ServerClient } from './generated/host';

/**
 * Represents all Chronicle gRPC services
 */
export interface ChronicleServices {
    /**
     * Event stores service
     */
    eventStores: EventStoresClient;

    /**
     * Namespaces service
     */
    namespaces: NamespacesClient;

    /**
     * Recommendations service
     */
    recommendations: RecommendationsClient;

    /**
     * Identities service
     */
    identities: IdentitiesClient;

    /**
     * Event sequences service
     */
    eventSequences: EventSequencesClient;

    /**
     * Event types service
     */
    eventTypes: EventTypesClient;

    /**
     * Constraints service
     */
    constraints: ConstraintsClient;

    /**
     * Observers service
     */
    observers: ObserversClient;

    /**
     * Failed partitions service
     */
    failedPartitions: FailedPartitionsClient;

    /**
     * Reactors service
     */
    reactors: ReactorsClient;

    /**
     * Reducers service
     */
    reducers: ReducersClient;

    /**
     * Projections service
     */
    projections: ProjectionsClient;

    /**
     * Read models service
     */
    readModels: ReadModelsClient;

    /**
     * Jobs service
     */
    jobs: JobsClient;

    /**
     * Event seeding service
     */
    eventSeeding: EventSeedingClient;

    /**
     * Server service
     */
    server: ServerClient;
}
