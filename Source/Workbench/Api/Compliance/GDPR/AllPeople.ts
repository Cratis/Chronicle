/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { Person } from './Person';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/compliance/gdpr/people');

class AllPeopleSortBy {
    private _id: SortingActionsForObservableQuery<Person[]>;
    private _socialSecurityNumber: SortingActionsForObservableQuery<Person[]>;
    private _firstName: SortingActionsForObservableQuery<Person[]>;
    private _lastName: SortingActionsForObservableQuery<Person[]>;
    private _address: SortingActionsForObservableQuery<Person[]>;
    private _city: SortingActionsForObservableQuery<Person[]>;
    private _postalCode: SortingActionsForObservableQuery<Person[]>;
    private _country: SortingActionsForObservableQuery<Person[]>;
    private _personalInformation: SortingActionsForObservableQuery<Person[]>;

    constructor(readonly query: AllPeople) {
        this._id = new SortingActionsForObservableQuery<Person[]>('id', query);
        this._socialSecurityNumber = new SortingActionsForObservableQuery<Person[]>('socialSecurityNumber', query);
        this._firstName = new SortingActionsForObservableQuery<Person[]>('firstName', query);
        this._lastName = new SortingActionsForObservableQuery<Person[]>('lastName', query);
        this._address = new SortingActionsForObservableQuery<Person[]>('address', query);
        this._city = new SortingActionsForObservableQuery<Person[]>('city', query);
        this._postalCode = new SortingActionsForObservableQuery<Person[]>('postalCode', query);
        this._country = new SortingActionsForObservableQuery<Person[]>('country', query);
        this._personalInformation = new SortingActionsForObservableQuery<Person[]>('personalInformation', query);
    }

    get id(): SortingActionsForObservableQuery<Person[]> {
        return this._id;
    }
    get socialSecurityNumber(): SortingActionsForObservableQuery<Person[]> {
        return this._socialSecurityNumber;
    }
    get firstName(): SortingActionsForObservableQuery<Person[]> {
        return this._firstName;
    }
    get lastName(): SortingActionsForObservableQuery<Person[]> {
        return this._lastName;
    }
    get address(): SortingActionsForObservableQuery<Person[]> {
        return this._address;
    }
    get city(): SortingActionsForObservableQuery<Person[]> {
        return this._city;
    }
    get postalCode(): SortingActionsForObservableQuery<Person[]> {
        return this._postalCode;
    }
    get country(): SortingActionsForObservableQuery<Person[]> {
        return this._country;
    }
    get personalInformation(): SortingActionsForObservableQuery<Person[]> {
        return this._personalInformation;
    }
}

class AllPeopleSortByWithoutQuery {
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

export class AllPeople extends ObservableQueryFor<Person[]> {
    readonly route: string = '/api/compliance/gdpr/people';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Person[] = [];
    private readonly _sortBy: AllPeopleSortBy;
    private static readonly _sortBy: AllPeopleSortByWithoutQuery = new AllPeopleSortByWithoutQuery();

    constructor() {
        super(Person, true);
        this._sortBy = new AllPeopleSortBy(this);
    }

    get requestArguments(): string[] {
        return [
        ];
    }

    get sortBy(): AllPeopleSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllPeopleSortByWithoutQuery {
        return this._sortBy;
    }

    static use(sorting?: Sorting): [QueryResultWithState<Person[]>, SetSorting] {
        return useObservableQuery<Person[], AllPeople>(AllPeople, undefined, sorting);
    }

    static useWithPaging(pageSize: number, sorting?: Sorting): [QueryResultWithState<Person[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<Person[], AllPeople>(AllPeople, new Paging(0, pageSize), undefined, sorting);
    }
}
