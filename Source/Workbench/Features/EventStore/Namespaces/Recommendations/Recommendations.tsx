// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { AllRecommendations, AllRecommendationsParameters } from 'Api/Recommendations';
import { DataTableFilterMeta } from 'primereact/datatable';
import { FilterMatchMode } from 'primereact/api';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { Column } from 'primereact/column';
import { Recommendation } from 'Api/Recommendations/Recommendation';
import { RecommendationsViewModel } from './RecommendationViewModel';
import * as faIcons from 'react-icons/fa6';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { DataPage, MenuItem } from 'Components';

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.IN },
};

const occurred = (recommendation: Recommendation) => {
    return recommendation.occurred.toLocaleString();
};

export const Recommendations = withViewModel(RecommendationsViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();

    const queryArgs: AllRecommendationsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    return (
        <DataPage
            title={strings.eventStore.namespaces.recommendations.title}
            query={AllRecommendations}
            queryArguments={queryArgs}
            onSelectionChange={(e) => (viewModel.selectedRecommendation = e.value as Recommendation)}
            dataKey='id'
            defaultFilters={defaultFilters}
            globalFilterFields={['tombstone']}
            emptyMessage={strings.eventStore.namespaces.recommendations.empty}>

            <DataPage.MenuItems>
                <MenuItem
                    id='perform'
                    label={strings.eventStore.namespaces.recommendations.actions.perform} icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => viewModel.perform()} />
                <MenuItem
                    id='ignore'
                    label={strings.eventStore.namespaces.recommendations.actions.ignore} icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => viewModel.ignore()} />
            </DataPage.MenuItems>

            <DataPage.Columns>
                <Column field='name' header={strings.eventStore.namespaces.recommendations.columns.name} sortable />
                <Column field='description' header={strings.eventStore.namespaces.recommendations.columns.description} />
                <Column field='occurred' header={strings.eventStore.namespaces.recommendations.columns.occurred} body={occurred} />
            </DataPage.Columns>
        </DataPage>);
});
