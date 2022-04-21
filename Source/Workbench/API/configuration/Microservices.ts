/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { Microservice } from './Microservice';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/configuration/microservices');

export class Microservices extends QueryFor<Microservice[]> {
    readonly route: string = '/api/configuration/microservices';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Microservice[] = [];

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResult<Microservice[]>, PerformQuery] {
        return useQuery<Microservice[], Microservices>(Microservices);
    }
}
