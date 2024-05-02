// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

import { ObservableQueryFor, QueryResultWithState, useObservableQuery } from 'Infrastructure/queries';
import { Person } from './Person';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/people');

export class AllPeople extends ObservableQueryFor<Person[]> {
    readonly route: string = '/api/compliance/gdpr/people';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Person[] = [];

    constructor() {
        super(Person, true);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    static use(): [QueryResultWithState<Person[]>] {
        return useObservableQuery<Person[], AllPeople>(AllPeople);
    }
}
