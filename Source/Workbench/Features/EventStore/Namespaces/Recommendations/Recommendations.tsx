// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Page } from 'Components/Common/Page';
import strings from 'Strings';
import { AllRecommendations, AllRecommendationsArguments } from 'Api/Recommendations';
import { DataTableFilterMeta } from 'primereact/datatable';
import { DataTableForObservableQuery } from 'Components/DataTables';
import { FilterMatchMode } from 'primereact/api';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { Column } from 'primereact/column';
import { RecommendationInformation } from 'Api/Concepts/Recommendations/RecommendationInformation';
import { RecommendationsViewModel } from './RecommendationViewModel';
import { MenuItem } from 'primereact/menuitem';
import { FaArrowsRotate } from 'react-icons/fa6';
import { Menubar } from 'primereact/menubar';
import { withViewModel } from '@cratis/applications.react.mvvm';

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.IN },
};

const occurred = (recommendation: RecommendationInformation) => {
    return recommendation.occurred.toLocaleString();
};

export const Recommendations = withViewModel(RecommendationsViewModel, ({viewModel}) => {
    const params = useParams<EventStoreAndNamespaceParams>();

    const queryArgs: AllRecommendationsArguments = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    const hasSelectedRecommendation = viewModel.selectedRecommendation !== undefined;

    const menuItems: MenuItem[] = [
        {
            id: 'replay',
            label: strings.eventStore.namespaces.recommendations.actions.reject,
            icon: <FaArrowsRotate className={'mr-2'} />,
            disabled: !hasSelectedRecommendation,
            command: () => viewModel.reject()
        }
    ];

    return (
        <Page title={strings.eventStore.namespaces.recommendations.title}>
            <div className="px-4 py-2">
                <Menubar aria-label='Actions' model={menuItems} />
            </div>

            <div className={'flex-1 overflow-hidden'}>
                <DataTableForObservableQuery
                    query={AllRecommendations}
                    queryArguments={queryArgs}
                    selection={viewModel.selectedRecommendation}
                    onSelectionChange={(e) => (viewModel.selectedRecommendation = e.value as RecommendationInformation)}

                    dataKey='id'
                    defaultFilters={defaultFilters}
                    globalFilterFields={['tombstone']}
                    emptyMessage={strings.eventStore.namespaces.recommendations.empty}>
                    <Column field='name' header={strings.eventStore.namespaces.recommendations.columns.name} sortable />
                    <Column field='description' header={strings.eventStore.namespaces.recommendations.columns.description} />
                    <Column field='occurred' header={strings.eventStore.namespaces.recommendations.columns.occurred} body={occurred} />
                </DataTableForObservableQuery>
            </div>

        </Page>);
});
