/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { ObserverInformation } from './ObserverInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/observers');

export interface GetObserversArguments {
    microserviceId: string;
    tenantId: string;
}
export class GetObservers extends QueryFor<ObserverInformation[], GetObserversArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/observers';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ObserverInformation[] = [];

    constructor() {
        super(ObserverInformation, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: GetObserversArguments): [QueryResultWithState<ObserverInformation[]>, PerformQuery<GetObserversArguments>] {
        return useQuery<ObserverInformation[], GetObservers, GetObserversArguments>(GetObservers, args);
    }
}
