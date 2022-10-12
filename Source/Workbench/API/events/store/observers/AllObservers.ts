/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from '@aksio/cratis-applications-frontend/queries';
import { ObserverState } from './ObserverState';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/events/store/observers?microserviceId={{microserviceId}}&tenantId={{tenantId}}');

export interface AllObserversArguments {
    microserviceId: string;
    tenantId: string;
}
export class AllObservers extends ObservableQueryFor<ObserverState[], AllObserversArguments> {
    readonly route: string = '/api/events/store/observers?microserviceId={{microserviceId}}&tenantId={{tenantId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: ObserverState[] = [];

    constructor() {
        super(ObserverState, true);
    }

    get requestArguments(): string[] {
        return [
            'microserviceId',
            'tenantId',
        ];
    }

    static use(args?: AllObserversArguments): [QueryResultWithState<ObserverState[]>] {
        return useObservableQuery<ObserverState[], AllObservers, AllObserversArguments>(AllObservers, args);
    }
}
