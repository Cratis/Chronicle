// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { AllRecommendations, AllRecommendationsParameters } from 'Features/Recommendations';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { RecommendationDetails } from 'Features/Recommendations';
import { RecommendationsViewModel } from './RecommendationViewModel';
import * as faIcons from 'react-icons/fa6';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { Column, DataPage, MenuItem } from '@cratis/components/DataPage';
import { Page } from 'Components/Common/Page';
import { SelectionCheckbox } from 'Components/Common/SelectionCheckbox';
import { useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';

const occurred = (recommendation: RecommendationDetails) => {
    return recommendation.occurred.toLocaleString();
};

export const Recommendations = withViewModel(RecommendationsViewModel, ({ viewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [showConfirmation] = useConfirmationDialog();

    const queryArgs: AllRecommendationsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };
    const [recommendations] = AllRecommendations.use(queryArgs);

    const handleIgnore = async () => {
        const recommendationIds = viewModel.selectedRecommendationIds;
        if (recommendationIds.length === 0) {
            return;
        }

        const isBulk = recommendationIds.length > 1;
        const singleRecommendation = isBulk
            ? undefined
            : recommendations.data.find(recommendation => recommendation.id.equals(recommendationIds[0]));

        const result = await showConfirmation(
            isBulk
                ? strings.eventStore.namespaces.recommendations.dialogs.ignoreRecommendations.title
                : strings.eventStore.namespaces.recommendations.dialogs.ignoreRecommendation.title,
            isBulk
                ? strings.eventStore.namespaces.recommendations.dialogs.ignoreRecommendations.message.replace('{count}', recommendationIds.length.toString())
                : strings.eventStore.namespaces.recommendations.dialogs.ignoreRecommendation.message.replace('{name}', singleRecommendation?.name ?? ''),
            DialogButtons.YesNo);

        if (result !== DialogResult.Yes) {
            return;
        }

        try {
            await viewModel.ignoreRecommendations(recommendationIds);
        } catch (error) {
            console.error('Failed to ignore recommendations:', error);
        }
    };

    return (
        <Page title={strings.eventStore.namespaces.recommendations.title}>
        <DataPage
            title={strings.eventStore.namespaces.recommendations.title}
            query={AllRecommendations}
            queryArguments={queryArgs}
            onSelectionChange={(e) => (viewModel.selectedRecommendation = e.value as RecommendationDetails)}
            dataKey='id'
            emptyMessage={strings.eventStore.namespaces.recommendations.empty}>

            <DataPage.MenuItems>
                <MenuItem
                    label={strings.eventStore.namespaces.recommendations.actions.selectAll} icon={faIcons.FaSquareCheck}
                    command={() => viewModel.selectAllRecommendations(recommendations.data.map((recommendation: RecommendationDetails) => recommendation.id))} />
                <MenuItem
                    label={strings.eventStore.namespaces.recommendations.actions.perform} icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => viewModel.perform()} />
                <MenuItem
                    label={strings.eventStore.namespaces.recommendations.actions.ignore} icon={faIcons.FaBan}
                    disabled={viewModel.selectedRecommendationIds.length === 0}
                    command={() => handleIgnore()} />
            </DataPage.MenuItems>

            <DataPage.Columns>
                <Column
                    body={(recommendation: RecommendationDetails) => (
                        <SelectionCheckbox
                            checked={viewModel.isRecommendationSelected(recommendation.id)}
                            onToggle={() => viewModel.toggleRecommendationSelection(recommendation.id)} />
                    )} />
                <Column field='name' header={strings.eventStore.namespaces.recommendations.columns.name} sortable />
                <Column field='description' header={strings.eventStore.namespaces.recommendations.columns.description} />
                <Column field='occurred' header={strings.eventStore.namespaces.recommendations.columns.occurred} body={occurred} />
            </DataPage.Columns>
        </DataPage>
        </Page>);
});
