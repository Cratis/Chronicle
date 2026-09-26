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
import { useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';
import { toast } from '@cratis/components/Notifications';

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

    const confirm = async (single: string, bulk: string, count: number, name: string) => {
        const isBulk = count > 1;
        const result = await showConfirmation(
            isBulk ? bulk : single,
            isBulk
                ? strings.eventStore.namespaces.recommendations.dialogs.ignoreRecommendations.message.replace('{count}', count.toString())
                : strings.eventStore.namespaces.recommendations.dialogs.ignoreRecommendation.message.replace('{name}', name),
            DialogButtons.YesNo);
        return result === DialogResult.Yes;
    };

    const handleIgnore = async () => {
        const selected = viewModel.selectedRecommendations;
        if (selected.length === 0) {
            return;
        }

        const confirmed = await confirm(
            strings.eventStore.namespaces.recommendations.dialogs.ignoreRecommendation.title,
            strings.eventStore.namespaces.recommendations.dialogs.ignoreRecommendations.title,
            selected.length,
            selected[0].name);

        if (!confirmed) {
            return;
        }

        // A failure here has to reach the user. Reporting it only to the console left the dialog
        // closing on an unchanged list with nothing said, which reads exactly like the action having
        // silently done nothing.
        const outcome = await viewModel.ignoreRecommendations(selected.map(recommendation => recommendation.id));
        if (outcome.failed === 0) {
            toast.success({ title: strings.eventStore.namespaces.recommendations.notifications.ignored });
        } else {
            toast.error({
                title: strings.eventStore.namespaces.recommendations.notifications.ignoreFailed,
                description: strings.eventStore.namespaces.recommendations.notifications.partialFailure
                    .replace('{failed}', outcome.failed.toString())
                    .replace('{total}', outcome.total.toString())
            });
        }
    };

    const handlePerform = async () => {
        const selected = viewModel.selectedRecommendations;
        if (selected.length === 0) {
            return;
        }

        const outcome = await viewModel.performRecommendations(selected.map(recommendation => recommendation.id));
        if (outcome.failed === 0) {
            toast.success({ title: strings.eventStore.namespaces.recommendations.notifications.performed });
        } else {
            toast.error({
                title: strings.eventStore.namespaces.recommendations.notifications.performFailed,
                description: strings.eventStore.namespaces.recommendations.notifications.partialFailure
                    .replace('{failed}', outcome.failed.toString())
                    .replace('{total}', outcome.total.toString())
            });
        }
    };

    return (
        <Page title={strings.eventStore.namespaces.recommendations.title}>
        <DataPage
            title={strings.eventStore.namespaces.recommendations.title}
            query={AllRecommendations}
            queryArguments={queryArgs}
            selectionMode='multiple'
            selectedItems={viewModel.selectedRecommendations}
            onSelectedItemsChange={(items) => (viewModel.selectedRecommendations = items as RecommendationDetails[])}
            dataKey='id'
            emptyMessage={strings.eventStore.namespaces.recommendations.empty}>

            <DataPage.MenuItems>
                <MenuItem
                    label={strings.eventStore.namespaces.recommendations.actions.perform} icon={faIcons.FaArrowsRotate}
                    disableOnUnselected
                    command={() => handlePerform()} />
                <MenuItem
                    label={strings.eventStore.namespaces.recommendations.actions.ignore} icon={faIcons.FaBan}
                    disableOnUnselected
                    command={() => handleIgnore()} />
            </DataPage.MenuItems>

            <DataPage.Columns>
                <Column selectionMode='multiple' />
                <Column field='name' header={strings.eventStore.namespaces.recommendations.columns.name} sortable />
                <Column field='description' header={strings.eventStore.namespaces.recommendations.columns.description} />
                <Column field='occurred' header={strings.eventStore.namespaces.recommendations.columns.occurred} body={occurred} />
            </DataPage.Columns>
        </DataPage>
        </Page>);
});
