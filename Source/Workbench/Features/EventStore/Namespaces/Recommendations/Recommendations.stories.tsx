// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { makeAutoObservable } from 'mobx';
import { observer } from 'mobx-react';
import { Guid } from '@cratis/fundamentals';
import { ObservableQueryFor, ObservableQuerySubscription, QueryResult, type OnNextResult } from '@cratis/arc/queries';
import { Toaster } from '@cratis/components/Notifications';
import { RecommendationDetails } from 'Features/Recommendations';
import { RecommendationsContent } from './Recommendations';
import { RecommendationsViewModel } from './RecommendationViewModel';

const recommendations = ['Replay changed projection', 'Recover failed observer'].map((name, index) =>
    Object.assign(new RecommendationDetails(), {
        id: Guid.parse(`00000000-0000-0000-0000-00000000000${index + 1}`), name,
        description: 'A simulated recommendation. Actions in this story never contact the backend.',
        occurred: new Date('2026-10-01T12:00:00Z')
    }));

class StoryRecommendations extends ObservableQueryFor<RecommendationDetails[]> {
    readonly route = '/storybook/recommendations';
    readonly defaultValue = recommendations;
    readonly parameterDescriptors = [];
    readonly requiredRequestParameters = [];

    constructor() { super(RecommendationDetails, true); }

    subscribe(callback: OnNextResult<QueryResult<RecommendationDetails[]>>) {
        callback(QueryResult.empty(recommendations));
        return new ObservableQuerySubscription<RecommendationDetails[]>({
            lastPingLatency: 0, averageLatency: 0, connect: () => undefined, disconnect: () => undefined
        });
    }
}

type SelectionStoryProps = { selected: number; failed: boolean };

const SelectionContent = observer(({ selected, failed }: SelectionStoryProps) => {
    const [viewModel] = useState(() => {
        const model = new RecommendationsViewModel({ eventStore: 'sample', namespace: 'default' });
        model.selectedRecommendations = recommendations.slice(0, selected);
        const apply = async (ids: Guid[]) => {
            await new Promise(resolve => setTimeout(resolve, 500));
            const failures = failed ? ['Simulated permission refusal. Failed targets remain selected.'] : [];
            model.selectedRecommendations = failed ? model.selectedRecommendations.filter(row => row.id.equals(ids[0])) : [];
            return { total: ids.length, failed: failures.length, failures };
        };
        model.performRecommendations = apply;
        model.ignoreRecommendations = apply;
        return makeAutoObservable(model);
    });
    return RecommendationsContent({ viewModel, recommendations: { isReady: true, isSuccess: true, isAuthorized: true, data: recommendations }, query: StoryRecommendations });
});

const SelectionStory = (props: SelectionStoryProps) => (
    <MemoryRouter initialEntries={['/sample/default']}>
        <Routes>
            <Route path='/:eventStore/:namespace' element={<SelectionContent {...props} />} />
        </Routes>
        <Toaster />
    </MemoryRouter>
);

const meta: Meta<typeof SelectionStory> = {
    title: 'Event Store/Recommendations/Selection and outcomes',
    component: SelectionStory,
    parameters: { layout: 'fullscreen' },
    args: { selected: 0, failed: false }
};
export default meta;

type Story = StoryObj<typeof SelectionStory>;
export const EmptySelection: Story = {};
export const SingleSelection: Story = { args: { selected: 1 } };
export const MultipleSelection: Story = { args: { selected: 2 } };
export const PartialFailure: Story = { args: { selected: 2, failed: true } };
