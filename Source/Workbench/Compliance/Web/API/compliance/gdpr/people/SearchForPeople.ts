/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { QueryFor, QueryResult, useQuery, PerformQuery } from '@aksio/frontend/queries';
import { Person } from './Person';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/people/search?query={{query}}');

export interface SearchForPeopleArguments {
    query: string;
}
export class SearchForPeople extends QueryFor<Person[], SearchForPeopleArguments> {
    readonly route: string = '/api/compliance/gdpr/people/search?query={{query}}';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Person[] = [];
    readonly requiresArguments: boolean = true;

    static use(args?: SearchForPeopleArguments): [QueryResult<Person[]>, PerformQuery<SearchForPeopleArguments>] {
        return useQuery<Person[], SearchForPeople, SearchForPeopleArguments>(SearchForPeople, args);
    }
}
