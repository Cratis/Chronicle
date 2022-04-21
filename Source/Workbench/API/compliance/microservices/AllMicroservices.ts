/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResult, useObservableQuery } from '@aksio/cratis-applications-frontend/queries';
import { Microservice } from './Microservice';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/microservices');

export class AllMicroservices extends ObservableQueryFor<Microservice[]> {
    readonly route: string = '/api/compliance/microservices';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Microservice[] = [];

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResult<Microservice[]>] {
        return useObservableQuery<Microservice[], AllMicroservices>(AllMicroservices);
    }
}
