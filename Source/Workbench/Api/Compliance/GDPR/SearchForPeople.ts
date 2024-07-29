/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

// eslint-disable-next-line header/header
import { QueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForQuery, Paging } from '@cratis/applications/queries';
import { useQuery, useQueryWithPaging, PerformQuery, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { Person } from './Person';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/people/search');

class SearchForPeopleSortBy {
    private _id: SortingActionsForQuery<Person[]>;
    private _socialSecurityNumber: SortingActionsForQuery<Person[]>;
    private _firstName: SortingActionsForQuery<Person[]>;
    private _lastName: SortingActionsForQuery<Person[]>;
    private _address: SortingActionsForQuery<Person[]>;
    private _city: SortingActionsForQuery<Person[]>;
    private _postalCode: SortingActionsForQuery<Person[]>;
    private _country: SortingActionsForQuery<Person[]>;
    private _personalInformation: SortingActionsForQuery<Person[]>;

    constructor(readonly query: SearchForPeople) {
        this._id = new SortingActionsForQuery<Person[]>('id', query);
        this._socialSecurityNumber = new SortingActionsForQuery<Person[]>('socialSecurityNumber', query);
        this._firstName = new SortingActionsForQuery<Person[]>('firstName', query);
        this._lastName = new SortingActionsForQuery<Person[]>('lastName', query);
        this._address = new SortingActionsForQuery<Person[]>('address', query);
        this._city = new SortingActionsForQuery<Person[]>('city', query);
        this._postalCode = new SortingActionsForQuery<Person[]>('postalCode', query);
        this._country = new SortingActionsForQuery<Person[]>('country', query);
        this._personalInformation = new SortingActionsForQuery<Person[]>('personalInformation', query);
    }

    get id(): SortingActionsForQuery<Person[]> {
        return this._id;
    }
    get socialSecurityNumber(): SortingActionsForQuery<Person[]> {
        return this._socialSecurityNumber;
    }
    get firstName(): SortingActionsForQuery<Person[]> {
        return this._firstName;
    }
    get lastName(): SortingActionsForQuery<Person[]> {
        return this._lastName;
    }
    get address(): SortingActionsForQuery<Person[]> {
        return this._address;
    }
    get city(): SortingActionsForQuery<Person[]> {
        return this._city;
    }
    get postalCode(): SortingActionsForQuery<Person[]> {
        return this._postalCode;
    }
    get country(): SortingActionsForQuery<Person[]> {
        return this._country;
    }
    get personalInformation(): SortingActionsForQuery<Person[]> {
        return this._personalInformation;
    }
}

class SearchForPeopleSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _socialSecurityNumber: SortingActions  = new SortingActions('socialSecurityNumber');
    private _firstName: SortingActions  = new SortingActions('firstName');
    private _lastName: SortingActions  = new SortingActions('lastName');
    private _address: SortingActions  = new SortingActions('address');
    private _city: SortingActions  = new SortingActions('city');
    private _postalCode: SortingActions  = new SortingActions('postalCode');
    private _country: SortingActions  = new SortingActions('country');
    private _personalInformation: SortingActions  = new SortingActions('personalInformation');

    get id(): SortingActions {
        return this._id;
    }
    get socialSecurityNumber(): SortingActions {
        return this._socialSecurityNumber;
    }
    get firstName(): SortingActions {
        return this._firstName;
    }
    get lastName(): SortingActions {
        return this._lastName;
    }
    get address(): SortingActions {
        return this._address;
    }
    get city(): SortingActions {
        return this._city;
    }
    get postalCode(): SortingActions {
        return this._postalCode;
    }
    get country(): SortingActions {
        return this._country;
    }
    get personalInformation(): SortingActions {
        return this._personalInformation;
    }
}

export interface SearchForPeopleArguments {
    query: string;
}

export class SearchForPeople extends QueryFor<Person[], SearchForPeopleArguments> {
    readonly route: string = '/api/compliance/gdpr/people/search';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Person[] = [];
    private readonly _sortBy: SearchForPeopleSortBy;
    private static readonly _sortBy: SearchForPeopleSortByWithoutQuery = new SearchForPeopleSortByWithoutQuery();

    constructor() {
        super(Person, true);
        this._sortBy = new SearchForPeopleSortBy(this);
    }

    get requestArguments(): string[] {
        return [
            'query',
        ];
    }

    get sortBy(): SearchForPeopleSortBy {
        return this._sortBy;
    }

    static get sortBy(): SearchForPeopleSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: SearchForPeopleArguments, sorting?: Sorting): [QueryResultWithState<Person[]>, PerformQuery<SearchForPeopleArguments>] {
        return useQuery<Person[], SearchForPeople, SearchForPeopleArguments>(SearchForPeople, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: SearchForPeopleArguments, sorting?: Sorting): [QueryResultWithState<Person[]>, number, PerformQuery, SetSorting, SetPage, SetPageSize] {
        return useQueryWithPaging<Person[], SearchForPeople>(SearchForPeople, new Paging(0, pageSize), args, sorting);
    }
}
