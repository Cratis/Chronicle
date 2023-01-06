/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { StorageForMicroservice } from './StorageForMicroservice';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/configuration/microservices/{{microserviceId}}/storage');

export interface storageForMicroserviceArguments {
    microserviceId: string;
}
export class storageForMicroservice extends QueryFor<StorageForMicroservice, storageForMicroserviceArguments> {
    readonly route: string = '/api/configuration/microservices/{{microserviceId}}/storage';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: StorageForMicroservice = {} as any;

    constructor() {
        super(StorageForMicroservice, false);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
        ];
    }

    static use(args?: storageForMicroserviceArguments): [QueryResultWithState<StorageForMicroservice>, PerformQuery<storageForMicroserviceArguments>] {
        return useQuery<StorageForMicroservice, storageForMicroservice, storageForMicroserviceArguments>(storageForMicroservice, args);
    }
}
