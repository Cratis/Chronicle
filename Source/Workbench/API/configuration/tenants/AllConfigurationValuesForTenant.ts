/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/configuration/tenants/{{tenantId}}');

export interface AllConfigurationValuesForTenantArguments {
    tenantId: string;
}
export class AllConfigurationValuesForTenant extends QueryFor<string[], AllConfigurationValuesForTenantArguments> {
    readonly route: string = '/api/configuration/tenants/{{tenantId}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: string[] = [];

    constructor() {
        super(String, true);
    }

    get requestArguments(): string[] {
        return [
            'tenantId',
        ];
    }

    static use(args?: AllConfigurationValuesForTenantArguments): [QueryResultWithState<string[]>, PerformQuery<AllConfigurationValuesForTenantArguments>] {
        return useQuery<string[], AllConfigurationValuesForTenant, AllConfigurationValuesForTenantArguments>(AllConfigurationValuesForTenant, args);
    }
}
