/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/applications/queries';
import { ConnectedClient } from './ConnectedClient';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/clients/{{microserviceId}}');

export interface ConnectedClientsForMicroserviceArguments {
    microserviceId: string;
}
export class ConnectedClientsForMicroservice extends ObservableQueryFor<ConnectedClient[], ConnectedClientsForMicroserviceArguments> {
    readonly route: string = '/api/clients/{{microserviceId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ConnectedClient[] = [];

    constructor() {
        super(ConnectedClient, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
        ];
    }

    static use(args?: ConnectedClientsForMicroserviceArguments): [QueryResultWithState<ConnectedClient[]>] {
        return useObservableQuery<ConnectedClient[], ConnectedClientsForMicroservice, ConnectedClientsForMicroserviceArguments>(ConnectedClientsForMicroservice, args);
    }
}
