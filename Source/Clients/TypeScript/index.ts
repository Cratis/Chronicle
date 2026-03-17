// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// ts-proto generates DeepPartial, MessageFns, and protobufPackage in every generated file.
// By explicitly claiming them here, subsequent export * statements silently skip the duplicates.
export type { DeepPartial, MessageFns } from './generated/clients';
export { protobufPackage } from './generated/clients';

// The following types are duplicated identically across multiple proto-generated files
// (the ProtoGenerator embeds shared types in each proto file rather than using imports).
// Explicit re-exports from canonical sources prevent TS2308 ambiguity from the export * below.
export { AppendedEvent, AppendedEvent_GenerationalContentEntry, Causation, Causation_PropertiesEntry, EventContext, EventObservationState, eventObservationStateFromJSON, eventObservationStateToJSON, EventRevision, SerializableDateTimeOffset } from './generated/eventsequences';
export { DateTimeOffset } from './generated/protobuf-net/bcl';
export { EventType } from './generated/events';
export { ConstraintType, constraintTypeFromJSON, constraintTypeToJSON } from './generated/events_constraints';
export { EventTypeWithKeyExpression, ObservationState, observationStateFromJSON, observationStateToJSON } from './generated/observation_reactors';
export { ObserverOwner, observerOwnerFromJSON, observerOwnerToJSON } from './generated/observation';
export { Identity } from './generated/identities';
export { IndexDefinition, ReadModelDefinition, ReadModelObserverType, readModelObserverTypeFromJSON, readModelObserverTypeToJSON, ReadModelOwner, readModelOwnerFromJSON, readModelOwnerToJSON, ReadModelSource, readModelSourceFromJSON, readModelSourceToJSON, ReadModelType, SinkDefinition } from './generated/readmodels';

// Export all generated gRPC services
export * from './generated/clients';
export * from './generated/cratis_chronicle_contracts';
export * from './generated/events';
export * from './generated/events_constraints';
export * from './generated/eventsequences';
export * from './generated/host';
export * from './generated/identities';
export * from './generated/jobs';
export * from './generated/observation';
export * from './generated/observation_reactors';
export * from './generated/observation_reducers';
export * from './generated/observation_webhooks';
export * from './generated/projections';
export * from './generated/readmodels';
export * from './generated/recommendations';
export * from './generated/security';
export * from './generated/seeding';
export * from './generated/protobuf-net/bcl';

// Export connection utilities
export * from './ChronicleConnection';
export * from './ChronicleConnectionString';
export * from './ChronicleServices';
export * from './TokenProvider';

