/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResultWithState, useQuery, PerformQuery } from 'Infrastructure/queries';
import { Person } from './Person';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/people/search/search');

export interface SearchForPeopleArguments {
    query: string;
}
export class SearchForPeople extends QueryFor<Person[], SearchForPeopleArguments> {
    readonly route: string = '/api/compliance/gdpr/people/search/search';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Person[] = [];

    constructor() {
        super(Person, true);
    }

    get requestArguments(): string[] {
        return [
            'query',
        ];
    }

    static use(args?: SearchForPeopleArguments): [QueryResultWithState<Person[]>, PerformQuery<SearchForPeopleArguments>] {
        return useQuery<Person[], SearchForPeople, SearchForPeopleArguments>(SearchForPeople, args);
    }
}
