/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { CreditCardApplication } from './CreditCardApplication';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/credit/applications');

export class Applications extends QueryFor<CreditCardApplication[]> {
    readonly route: string = '/api/accounts/credit/applications';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: CreditCardApplication[] = [];

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResult<CreditCardApplication[]>, PerformQuery] {
        return useQuery<CreditCardApplication[], Applications>(Applications);
    }
}
