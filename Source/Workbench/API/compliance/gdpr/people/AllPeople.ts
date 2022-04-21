/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResult, useObservableQuery } from '@aksio/cratis-applications-frontend/queries';
import { Person } from './Person';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/people');

export class AllPeople extends ObservableQueryFor<Person[]> {
    readonly route: string = '/api/compliance/gdpr/people';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Person[] = [];

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResult<Person[]>] {
        return useObservableQuery<Person[], AllPeople>(AllPeople);
    }
}
