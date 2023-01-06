/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { StorageForMicroservice } from './StorageForMicroservice';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/configuration/microservices/{{microserviceId}}/storage');

export interface StorageConfigurationForMicroserviceArguments {
    microserviceId: string;
}
export class StorageConfigurationForMicroservice extends QueryFor<StorageForMicroservice, StorageConfigurationForMicroserviceArguments> {
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

    static use(args?: StorageConfigurationForMicroserviceArguments): [QueryResultWithState<StorageForMicroservice>, PerformQuery<StorageConfigurationForMicroserviceArguments>] {
        return useQuery<StorageForMicroservice, StorageConfigurationForMicroservice, StorageConfigurationForMicroserviceArguments>(StorageConfigurationForMicroservice, args);
    }
}
