/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/cratis-applications-frontend/queries';
import { PotentialMoneyLaundryCase } from './PotentialMoneyLaundryCase';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/moneylaundring');

export class AllPotentialMoneyLaundryCases extends QueryFor<PotentialMoneyLaundryCase[]> {
    readonly route: string = '/api/accounts/debit/moneylaundring';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: PotentialMoneyLaundryCase[] = [];

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResult<PotentialMoneyLaundryCase[]>, PerformQuery] {
        return useQuery<PotentialMoneyLaundryCase[], AllPotentialMoneyLaundryCases>(AllPotentialMoneyLaundryCases);
    }
}
