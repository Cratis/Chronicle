// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import strings from 'Strings';
import { AllRecommendations, AllRecommendationsParameters, RecommendationDetails, IgnoredRecommendations } from 'Features/Recommendations';
import { type EventStoreAndNamespaceParams } from 'Shared';
import { useParams } from 'react-router-dom';
import { RecommendationsViewModel } from './RecommendationViewModel';
import * as faIcons from 'react-icons/fa6';
import { withViewModel } from '@cratis/arc.react.mvvm';
import { Column, DataPage, MenuItem } from '@cratis/components/DataPage';
import { Page } from 'Components/Common/Page';
import { useConfirmationDialog, DialogResult, DialogButtons } from '@cratis/arc.react/dialogs';
import { toast } from '@cratis/components/Notifications';
import { useEffect } from 'react';
import { type Constructor } from '@cratis/fundamentals';
import { type IObservableQueryFor, type QueryResult } from '@cratis/arc/queries';

const occurred = (recommendation: RecommendationDetails) => recommendation.occurred.toLocaleString();

type RecommendationsContentProps = {
    viewModel: RecommendationsViewModel;
    recommendations: Pick<QueryResult<RecommendationDetails[]>, 'data' | 'isReady' | 'isSuccess' | 'isAuthorized'>;
    query: Constructor<IObservableQueryFor<RecommendationDetails[], AllRecommendationsParameters>>;
};

export const RecommendationsContent = ({ viewModel, recommendations, query }: RecommendationsContentProps) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const [showConfirmation] = useConfirmationDialog();
    const queryArgs: AllRecommendationsParameters = {
        eventStore: params.eventStore!,
        namespace: params.namespace!
    };
    const notifications = strings.eventStore.namespaces.recommendations.notifications;

    const canReconcile = recommendations.isReady && recommendations.isSuccess && recommendations.isAuthorized;
    useEffect(() => {
        if (canReconcile) viewModel.reconcileSelection(recommendations.data);
    }, [recommendations.data, canReconcile, viewModel]);

    const confirmIgnore = async (selected: RecommendationDetails[], unignore: boolean) => {
        const dialogs = strings.eventStore.namespaces.recommendations.dialogs;
        const dialog = unignore
            ? (selected.length > 1 ? dialogs.unignoreRecommendations : dialogs.unignoreRecommendation)
            : (selected.length > 1 ? dialogs.ignoreRecommendations : dialogs.ignoreRecommendation);
        const message = dialog.message.replace('{count}', selected.length.toString()).replace('{name}', selected[0].name);
        return await showConfirmation(dialog.title, message, DialogButtons.YesNo) === DialogResult.Yes;
    };

    const notifyOutcome = (outcome: Awaited<ReturnType<RecommendationsViewModel['performRecommendations']>>, ignore: boolean, unignore: boolean) => {
        const description = notifications.outcome.replace('{failed}', outcome.failed.toString())
            .replace('{total}', outcome.total.toString()).replace('{succeeded}', (outcome.total - outcome.failed).toString());
        if (outcome.failed === 0) {
            toast.success({ title: unignore ? notifications.unignored : ignore ? notifications.ignored : notifications.performed, description });
        } else {
            toast.error({ title: unignore ? notifications.unignoreFailed : ignore ? notifications.ignoreFailed : notifications.performFailed,
                description: `${description}\n${outcome.failures.join('\n')}` });
        }
    };

    const handleAction = async (ignore: boolean, unignore = false) => {
        if (viewModel.isBusy || viewModel.selectedRecommendations.length === 0) return;
        const selected = [...viewModel.selectedRecommendations];
        const recommendationIds = selected.map(recommendation => recommendation.id);
        viewModel.isBusy = true;
        try {
            if ((ignore || unignore) && !await confirmIgnore(selected, unignore)) return;
            const outcome = await (unignore
                ? viewModel.unignoreRecommendations(recommendationIds)
                : ignore ? viewModel.ignoreRecommendations(recommendationIds)
                    : viewModel.performRecommendations(recommendationIds));
            notifyOutcome(outcome, ignore, unignore);
        } catch (error) {
            toast.error({ title: unignore ? notifications.unignoreFailed : ignore ? notifications.ignoreFailed : notifications.performFailed,
                description: error instanceof Error ? error.message : notifications.unknownError });
        } finally {
            viewModel.isBusy = false;
        }
    };

    return (
        <Page title={strings.eventStore.namespaces.recommendations.title}>
            <div role='status'>{viewModel.isBusy ? notifications.working : ''}</div>
            <DataPage
                key={viewModel.showIgnored ? 'ignored' : 'active'}
                title={strings.eventStore.namespaces.recommendations.title}
                query={query}
                queryArguments={queryArgs}
                selectionMode='multiple'
                selectedItems={viewModel.selectedRecommendations}
                onSelectedItemsChange={items => { viewModel.selectedRecommendations = items; }}
                dataKey='id'
                emptyMessage={viewModel.showIgnored ? strings.eventStore.namespaces.recommendations.emptyIgnored : strings.eventStore.namespaces.recommendations.empty}>
                <DataPage.MenuItems>
                    <MenuItem
                        label={strings.eventStore.namespaces.recommendations.actions.perform} icon={faIcons.FaArrowsRotate}
                        disabled={viewModel.showIgnored || viewModel.isBusy || viewModel.selectedRecommendations.length === 0}
                        command={() => handleAction(false)} />
                    <MenuItem
                        label={strings.eventStore.namespaces.recommendations.actions.ignore} icon={faIcons.FaBan}
                        disabled={viewModel.showIgnored || viewModel.isBusy || viewModel.selectedRecommendations.length === 0}
                        command={() => handleAction(true)} />
                    <MenuItem
                        label={strings.eventStore.namespaces.recommendations.actions.unignore} icon={faIcons.FaArrowRotateLeft}
                        disabled={!viewModel.showIgnored || viewModel.isBusy || viewModel.selectedRecommendations.length === 0}
                        command={() => handleAction(false, true)} />
                    <MenuItem
                        label={viewModel.showIgnored ? strings.eventStore.namespaces.recommendations.actions.showActive : strings.eventStore.namespaces.recommendations.actions.showIgnored}
                        icon={faIcons.FaEye} disabled={viewModel.isBusy}
                        command={() => viewModel.toggleIgnoredView()} />
                </DataPage.MenuItems>
                <DataPage.Columns>
                    <Column field='name' header={strings.eventStore.namespaces.recommendations.columns.name} sortable />
                    <Column field='description' header={strings.eventStore.namespaces.recommendations.columns.description} />
                    <Column field='occurred' header={strings.eventStore.namespaces.recommendations.columns.occurred} body={occurred} />
                </DataPage.Columns>
            </DataPage>
        </Page>);
};

const RecommendationsPage = ({ viewModel }: { viewModel: RecommendationsViewModel }) => {
    const params = useParams<EventStoreAndNamespaceParams>();
    const queryArgs = { eventStore: params.eventStore!, namespace: params.namespace! };
    const [activeRecommendations] = AllRecommendations.use(queryArgs);
    const [ignoredRecommendations] = IgnoredRecommendations.use(queryArgs);
    const recommendations = viewModel.showIgnored ? ignoredRecommendations : activeRecommendations;
    const query = viewModel.showIgnored ? IgnoredRecommendations : AllRecommendations;
    // Keep observable reads inside withViewModel's tracked render, including the shared content.
    return RecommendationsContent({ viewModel, recommendations, query });
};

export const Recommendations = withViewModel(RecommendationsViewModel, RecommendationsPage);
