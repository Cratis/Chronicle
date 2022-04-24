/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { Tenant } from './Tenant';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/configuration/tenants');

export class Tenants extends QueryFor<Tenant[]> {
    readonly route: string = '/api/configuration/tenants';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Tenant[] = [];

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResult<Tenant[]>, PerformQuery] {
        return useQuery<Tenant[], Tenants>(Tenants);
    }
}
