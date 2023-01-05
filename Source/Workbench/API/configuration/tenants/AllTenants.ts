/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { TenantInfo } from './TenantInfo';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/configuration/tenants');

export class AllTenants extends QueryFor<TenantInfo[]> {
    readonly route: string = '/api/configuration/tenants';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: TenantInfo[] = [];

    constructor() {
        super(TenantInfo, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<TenantInfo[]>, PerformQuery] {
        return useQuery<TenantInfo[], AllTenants>(AllTenants);
    }
}
