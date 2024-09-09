// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { AllRecommendations, AllRecommendationsArguments } from 'Api/Recommendations';
import { DataTableFilterMeta } from 'primereact/datatable';
import { FilterMatchMode } from 'primereact/api';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { Column } from 'primereact/column';
import { RecommendationInformation } from 'Api/Concepts/Recommendations/RecommendationInformation';
import { RecommendationsViewModel } from './RecommendationViewModel';
import * as faIcons from 'react-icons/fa6';
import { withViewModel } from '@cratis/applications.react.mvvm';
import { DataPage, MenuItem } from 'Components';

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.IN },
};

const occurred = (recommendation: RecommendationInformation) => {
    return recommendation.occurred.toLocaleString();
};

export const Recommendations = withViewModel(RecommendationsViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();

    const queryArgs: AllRecommendationsArguments = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    return (
        <DataPage
            title={strings.eventStore.namespaces.recommendations.title}
            query={AllRecommendations}
            queryArguments={queryArgs}
            onSelectionChange={(e) => (viewModel.selectedRecommendation = e.value as RecommendationInformation)}
            dataKey='id'
            defaultFilters={defaultFilters}
            globalFilterFields={['tombstone']}
            emptyMessage={strings.eventStore.namespaces.recommendations.empty}>

            <DataPage.MenuItems>
                <MenuItem
                    id='reject'
                    label={strings.eventStore.namespaces.recommendations.actions.reject} icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => viewModel.reject()} />
            </DataPage.MenuItems>

            <DataPage.Columns>
                <Column field='name' header={strings.eventStore.namespaces.recommendations.columns.name} sortable />
                <Column field='description' header={strings.eventStore.namespaces.recommendations.columns.description} />
                <Column field='occurred' header={strings.eventStore.namespaces.recommendations.columns.occurred} body={occurred} />
            </DataPage.Columns>
        </DataPage>);
});
