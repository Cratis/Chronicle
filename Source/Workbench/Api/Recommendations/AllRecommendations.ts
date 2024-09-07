/*---------------------------------------------------------------------------------------------
 *  **DO NOT EDIT** - This file is an automatically generated file.
 *--------------------------------------------------------------------------------------------*/

/* eslint-disable sort-imports */
// eslint-disable-next-line header/header
import { ObservableQueryFor, QueryResultWithState, Sorting, SortingActions, SortingActionsForObservableQuery, Paging } from '@cratis/applications/queries';
import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/applications.react/queries';
import { RecommendationInformation } from '../Concepts/Recommendations/RecommendationInformation';
import Handlebars from 'handlebars';

const routeTemplate = Handlebars.compile('/api/event-store/{{eventStore}}/{{namespace}}/recommendations/observe');

class AllRecommendationsSortBy {
    private _id: SortingActionsForObservableQuery<RecommendationInformation[]>;
    private _name: SortingActionsForObservableQuery<RecommendationInformation[]>;
    private _description: SortingActionsForObservableQuery<RecommendationInformation[]>;
    private _type: SortingActionsForObservableQuery<RecommendationInformation[]>;
    private _occurred: SortingActionsForObservableQuery<RecommendationInformation[]>;

    constructor(readonly query: AllRecommendations) {
        this._id = new SortingActionsForObservableQuery<RecommendationInformation[]>('id', query);
        this._name = new SortingActionsForObservableQuery<RecommendationInformation[]>('name', query);
        this._description = new SortingActionsForObservableQuery<RecommendationInformation[]>('description', query);
        this._type = new SortingActionsForObservableQuery<RecommendationInformation[]>('type', query);
        this._occurred = new SortingActionsForObservableQuery<RecommendationInformation[]>('occurred', query);
    }

    get id(): SortingActionsForObservableQuery<RecommendationInformation[]> {
        return this._id;
    }
    get name(): SortingActionsForObservableQuery<RecommendationInformation[]> {
        return this._name;
    }
    get description(): SortingActionsForObservableQuery<RecommendationInformation[]> {
        return this._description;
    }
    get type(): SortingActionsForObservableQuery<RecommendationInformation[]> {
        return this._type;
    }
    get occurred(): SortingActionsForObservableQuery<RecommendationInformation[]> {
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
export class AllRecommendations extends ObservableQueryFor<RecommendationInformation[], AllRecommendationsArguments> {
    readonly route: string = '/api/event-store/{eventStore}/{namespace}/recommendations/observe';
    readonly routeTemplate: Handlebars.TemplateDelegate = routeTemplate;
    readonly defaultValue: RecommendationInformation[] = [];
    private readonly _sortBy: AllRecommendationsSortBy;
    private static readonly _sortBy: AllRecommendationsSortByWithoutQuery = new AllRecommendationsSortByWithoutQuery();

    constructor() {
        super(RecommendationInformation, true);
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

    static use(args?: AllRecommendationsArguments, sorting?: Sorting): [QueryResultWithState<RecommendationInformation[]>, SetSorting] {
        return useObservableQuery<RecommendationInformation[], AllRecommendations, AllRecommendationsArguments>(AllRecommendations, args, sorting);
    }

    static useWithPaging(pageSize: number, args?: AllRecommendationsArguments, sorting?: Sorting): [QueryResultWithState<RecommendationInformation[]>, SetSorting, SetPage, SetPageSize] {
        return useObservableQueryWithPaging<RecommendationInformation[], AllRecommendations>(AllRecommendations, new Paging(0, pageSize), args, sorting);
    }
}
