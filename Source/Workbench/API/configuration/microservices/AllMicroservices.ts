/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { Microservice } from './Microservice';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/configuration/microservices');

export class AllMicroservices extends QueryFor<Microservice[]> {
    readonly route: string = '/api/configuration/microservices';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Microservice[] = [];

    constructor() {
        super(Microservice, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<Microservice[]>, PerformQuery] {
        return useQuery<Microservice[], AllMicroservices>(AllMicroservices);
    }
}
