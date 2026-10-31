// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { AllRecommendations, AllRecommendationsParameters, IgnoredRecommendations } from 'Features/Recommendations';
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
    const [activeRecommendations] = AllRecommendations.use(queryArgs);
    const [ignoredRecommendations] = IgnoredRecommendations.use(queryArgs);
    const recommendations = viewModel.showIgnored ? ignoredRecommendations : activeRecommendations;
    // Both queries return the same RecommendationDetails[] and take the same parameters; only the generated
    // private sort helpers differ, which the DataPage constructor type cannot see through.
    const query = (viewModel.showIgnored ? IgnoredRecommendations : AllRecommendations) as typeof AllRecommendations;
    const strs = strings.eventStore.namespaces.recommendations;

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

    const handleUnignore = async () => {
        const recommendationIds = viewModel.selectedRecommendationIds;
        if (recommendationIds.length === 0) {
            return;
        }

        const isBulk = recommendationIds.length > 1;
        const singleRecommendation = isBulk
            ? undefined
            : recommendations.data.find(recommendation => recommendation.id.equals(recommendationIds[0]));

        const result = await showConfirmation(
            isBulk ? strs.dialogs.unignoreRecommendations.title : strs.dialogs.unignoreRecommendation.title,
            isBulk
                ? strs.dialogs.unignoreRecommendations.message.replace('{count}', recommendationIds.length.toString())
                : strs.dialogs.unignoreRecommendation.message.replace('{name}', singleRecommendation?.name ?? ''),
            DialogButtons.YesNo);

        if (result !== DialogResult.Yes) {
            return;
        }

        try {
            await viewModel.unignoreRecommendations(recommendationIds);
        } catch (error) {
            console.error('Failed to unignore recommendations:', error);
        }
    };

    return (
        <Page title={strings.eventStore.namespaces.recommendations.title}>
        <DataPage
            key={viewModel.showIgnored ? 'ignored' : 'active'}
            title={strings.eventStore.namespaces.recommendations.title}
            query={query}
            queryArguments={queryArgs}
            onSelectionChange={(e) => (viewModel.selectedRecommendation = e.value as RecommendationDetails)}
            dataKey='id'
            emptyMessage={viewModel.showIgnored ? strs.emptyIgnored : strs.empty}>

            <DataPage.MenuItems>
                <MenuItem
                    label={strings.eventStore.namespaces.recommendations.actions.selectAll} icon={faIcons.FaSquareCheck}
                    command={() => viewModel.selectAllRecommendations(recommendations.data.map((recommendation: RecommendationDetails) => recommendation.id))} />
                <MenuItem
                    label={viewModel.showIgnored ? strs.actions.showActive : strs.actions.showIgnored}
                    icon={viewModel.showIgnored ? faIcons.FaList : faIcons.FaEyeSlash}
                    command={() => viewModel.toggleIgnoredView()} />
                {!viewModel.showIgnored && (
                    <MenuItem
                        label={strs.actions.perform} icon={faIcons.FaArrowsRotate}
                        disableOnUnselected
                        command={() => viewModel.perform()} />
                )}
                {!viewModel.showIgnored && (
                    <MenuItem
                        label={strs.actions.ignore} icon={faIcons.FaBan}
                        disabled={viewModel.selectedRecommendationIds.length === 0}
                        command={() => handleIgnore()} />
                )}
                {viewModel.showIgnored && (
                    <MenuItem
                        label={strs.actions.unignore} icon={faIcons.FaRotateLeft}
                        disabled={viewModel.selectedRecommendationIds.length === 0}
                        command={() => handleUnignore()} />
                )}
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
