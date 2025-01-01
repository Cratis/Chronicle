/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { Recommendation } from './Recommendation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/recommendations/all-recommendations/observe');

class AllRecommendationsSortBy {
    private _id: SortingActionsForObservableQuery<Recommendation[]>;
    private _name: SortingActionsForObservableQuery<Recommendation[]>;
    private _description: SortingActionsForObservableQuery<Recommendation[]>;
    private _type: SortingActionsForObservableQuery<Recommendation[]>;
    private _occurred: SortingActionsForObservableQuery<Recommendation[]>;

    constructor(readonly query: AllRecommendations) {
        this._id = new SortingActionsForObservableQuery<Recommendation[]>('id', query);
        this._name = new SortingActionsForObservableQuery<Recommendation[]>('name', query);
        this._description = new SortingActionsForObservableQuery<Recommendation[]>('description', query);
        this._type = new SortingActionsForObservableQuery<Recommendation[]>('type', query);
        this._occurred = new SortingActionsForObservableQuery<Recommendation[]>('occurred', query);
    }

    get id(): SortingActionsForObservableQuery<Recommendation[]> {
        return this._id;
    }
    get name(): SortingActionsForObservableQuery<Recommendation[]> {
        return this._name;
    }
    get description(): SortingActionsForObservableQuery<Recommendation[]> {
        return this._description;
    }
    get type(): SortingActionsForObservableQuery<Recommendation[]> {
        return this._type;
    }
    get occurred(): SortingActionsForObservableQuery<Recommendation[]> {
        return this._occurred;
    }
}

class AllRecommendationsSortByWithoutQuery {
    private _id: SortingActions  = new SortingActions('id');
    private _name: SortingActions  = new SortingActions('name');
    private _description: SortingActions  = new SortingActions('description');
    private _type: SortingActions  = new SortingActions('type');
    private _occurred: SortingActions  = new SortingActions('occurred');

    get id(): SortingActions {
        return this._id;
    }
    get name(): SortingActions {
        return this._name;
    }
    get description(): SortingActions {
        return this._description;
    }
    get type(): SortingActions {
        return this._type;
    }
    get occurred(): SortingActions {
        return this._occurred;
    }
}

export interface AllRecommendationsArguments {
    eventStore: string;
    namespace: string;
}
export class AllRecommendations extends ObservableQueryFor<Recommendation[], AllRecommendationsArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/recommendations/all-recommendations/observe';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: Recommendation[] = [];
    private readonly _sortBy: AllRecommendationsSortBy;
    private static readonly _sortBy: AllRecommendationsSortByWithoutQuery = new AllRecommendationsSortByWithoutQuery();

    constructor() {
        super(Recommendation, true);
        this._sortBy = new AllRecommendationsSortBy(this);
    }

    get requiredRequestArguments(): string[] {
        return [
            'eventStore',
            'namespace',
        ];
    }

    get sortBy(): AllRecommendationsSortBy {
        return this._sortBy;
    }

    static get sortBy(): AllRecommendationsSortByWithoutQuery {
        return this._sortBy;
    }

    static use(args?: AllRecommendationsArguments, sorting?: Sorting): [QueryResultWithState<Recommendation[]>, SetSorting] {
        return useObservableQuery<Recommendation[], AllRecommendations, AllRecommendationsArguments>(AllRecommendations, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllRecommendationsArguments, sorting?: Sorting): [QueryResultWithState<Recommendation[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<Recommendation[], AllRecommendations>(AllRecommendations, new Paging(0, pageSize), args, sorting);
    }
}
