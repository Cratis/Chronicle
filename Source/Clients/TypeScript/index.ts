import * as protoLoader from '@grpc/proto-loader';
import * as grpc from '@grpc/grpc-js';
import { resolve, dirname, join } from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

/**
 * Loads Chronicle gRPC proto definitions from the bundled proto files
 * @returns Package definition object
 */
export function loadChronicleProtos() {
    const protoPath = join(__dirname, '../proto');
    
    const packageDefinition = protoLoader.loadSync(
        [
            resolve(protoPath, 'clients.proto'),
            resolve(protoPath, 'cratis_chronicle_contracts.proto'),
            resolve(protoPath, 'events.proto'),
            resolve(protoPath, 'events_constraints.proto'),
            resolve(protoPath, 'eventsequences.proto'),
            resolve(protoPath, 'host.proto'),
            resolve(protoPath, 'identities.proto'),
            resolve(protoPath, 'jobs.proto'),
            resolve(protoPath, 'observation.proto'),
            resolve(protoPath, 'observation_reactors.proto'),
            resolve(protoPath, 'observation_reducers.proto'),
            resolve(protoPath, 'projections.proto'),
            resolve(protoPath, 'readmodels.proto'),
            resolve(protoPath, 'recommendations.proto'),
            resolve(protoPath, 'seeding.proto'),
        ],
        {
            keepCase: true,
            longs: String,
            enums: String,
            defaults: true,
            oneofs: true,
            includeDirs: [protoPath]
        }
    );

    return grpc.loadPackageDefinition(packageDefinition);
}

/**
 * Loads Chronicle gRPC proto definitions from a custom path
 * @param protoPath - Path to the directory containing .proto files
 * @returns Package definition object
 */
export function loadChronicleProtosFromPath(protoPath: string) {
    const packageDefinition = protoLoader.loadSync(
        [
            resolve(protoPath, 'clients.proto'),
            resolve(protoPath, 'cratis_chronicle_contracts.proto'),
            resolve(protoPath, 'events.proto'),
            resolve(protoPath, 'events_constraints.proto'),
            resolve(protoPath, 'eventsequences.proto'),
            resolve(protoPath, 'host.proto'),
            resolve(protoPath, 'identities.proto'),
            resolve(protoPath, 'jobs.proto'),
            resolve(protoPath, 'observation.proto'),
            resolve(protoPath, 'observation_reactors.proto'),
            resolve(protoPath, 'observation_reducers.proto'),
            resolve(protoPath, 'projections.proto'),
            resolve(protoPath, 'readmodels.proto'),
            resolve(protoPath, 'recommendations.proto'),
            resolve(protoPath, 'seeding.proto'),
        ],
        {
            keepCase: true,
            longs: String,
            enums: String,
            defaults: true,
            oneofs: true,
            includeDirs: [protoPath]
        }
    );

    return grpc.loadPackageDefinition(packageDefinition);
}

export { grpc };
