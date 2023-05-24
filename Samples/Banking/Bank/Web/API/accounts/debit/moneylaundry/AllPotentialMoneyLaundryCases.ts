/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from '@aksio/applications/queries';
import { PotentialMoneyLaundryCase } from './PotentialMoneyLaundryCase';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/accounts/debit/moneylaundry');

export class AllPotentialMoneyLaundryCases extends QueryFor<PotentialMoneyLaundryCase[]> {
    readonly route: string = '/api/accounts/debit/moneylaundry';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: PotentialMoneyLaundryCase[] = [];

    constructor() {
        super(PotentialMoneyLaundryCase, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<PotentialMoneyLaundryCase[]>, PerformQuery] {
        return useQuery<PotentialMoneyLaundryCase[], AllPotentialMoneyLaundryCases>(AllPotentialMoneyLaundryCases);
    }
}
