/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/applications/queries';
import { ObserverInformation } from './ObserverInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/{{microserviceId}}/{{tenantId}}/observers/observe');

export interface AllObserversArguments {
    microserviceId: string;
    tenantId: string;
}
export class AllObservers extends ObservableQueryFor<ObserverInformation[], AllObserversArguments> {
    readonly route: string = '/api/events/store/{{microserviceId}}/{{tenantId}}/observers/observe';
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

    static use(args?: AllObserversArguments): [QueryResultWithState<ObserverInformation[]>] {
        return useObservableQuery<ObserverInformation[], AllObservers, AllObserversArguments>(AllObservers, args);
    }
}
