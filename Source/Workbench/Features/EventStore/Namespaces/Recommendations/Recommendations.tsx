// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { AllRecommendations, AllRecommendationsParameters } from 'Api/Recommendations';
import { type DataTableFilterMeta } from '@cratis/components/DataTables';
import { FilterMatchMode } from '@primereact/headless/datatable';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { Recommendation } from 'Api/Recommendations/Recommendation';
import { RecommendationsViewModel } from './RecommendationViewModel';
import * as faIcons from 'react-icons/fa6';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { Column, DataPage, MenuItem } from '@cratis/components/DataPage';
import { Page } from 'Components/Common/Page';
import { useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';

const defaultFilters: DataTableFilterMeta = {
    tombstone: { value: null, matchMode: FilterMatchMode.In },
};

const occurred = (recommendation: Recommendation) => {
    return recommendation.occurred.toLocaleString();
};

export const Recommendations = withViewModel(RecommendationsViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [showConfirmation] = useConfirmationDialog();

    const queryArgs: AllRecommendationsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };

    const handleIgnore = async () => {
        if (viewModel.selectedRecommendation) {
            const result = await showConfirmation(
                strings.eventStore.namespaces.recommendations.dialogs.ignoreRecommendation.title,
                strings.eventStore.namespaces.recommendations.dialogs.ignoreRecommendation.message.replace('{name}', viewModel.selectedRecommendation.name),
                DialogButtons.YesNo
            );

            if (result === DialogResult.Yes) {
                await viewModel.ignore();
            }
        }
    };

    return (
        <Page title={strings.eventStore.namespaces.recommendations.title}>
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
                    label={strings.eventStore.namespaces.recommendations.actions.perform} icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => viewModel.perform()} />
                <MenuItem
                    label={strings.eventStore.namespaces.recommendations.actions.ignore} icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => handleIgnore()} />
            </DataPage.MenuItems>

            <DataPage.Columns>
                <Column field='name' header={strings.eventStore.namespaces.recommendations.columns.name} sortable />
                <Column field='description' header={strings.eventStore.namespaces.recommendations.columns.description} />
                <Column field='occurred' header={strings.eventStore.namespaces.recommendations.columns.occurred} body={occurred} />
            </DataPage.Columns>
        </DataPage>
        </Page>);
});
