// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, createElement, type ReactElement, type ReactNode } from 'react';
import { vi } from 'vitest';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ArcContext } from '@cratis/arc.react';
import { QueryInstanceCacheContext } from '@cratis/arc.react/queries';
import { CommandResult } from '@cratis/arc/commands';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { QueryInstanceCache, QueryResult, type ObservableQuerySubscription } from '@cratis/arc/queries';
import { Guid } from '@cratis/fundamentals';
import { type DataPageProps, type MenuItemProps } from '@cratis/components/DataPage';
import { AllRecommendations, IgnoredRecommendations, PerformRecommendation, RecommendationDetails, UnignoreRecommendation } from 'Features/Recommendations';
import { Recommendations } from '../../Recommendations';
import { an_empty_root } from './an_empty_root';

const mocks = vi.hoisted(() => {
    vi.resetModules();
    return { table: vi.fn(), page: vi.fn(), confirm: vi.fn(), success: vi.fn(), error: vi.fn(),
        params: { eventStore: 'some-event-store', namespace: 'some-namespace' } };
});
vi.unmock('react');
vi.unmock('@cratis/arc.react.mvvm');
vi.unmock('react-router-dom');
vi.mock('@cratis/arc.react/dialogs', async importOriginal => ({
    ...await importOriginal<typeof import('@cratis/arc.react/dialogs')>(), useConfirmationDialog: () => [mocks.confirm]
}));
vi.mock('@cratis/components/Notifications', () => ({ toast: { success: mocks.success, error: mocks.error } }));
vi.mock('@cratis/components/DataPage', () => ({
    DataPage: Object.assign((props: unknown) => { mocks.table(props); return null; }, { MenuItems: () => null, Columns: () => null }),
    MenuItem: () => null, Column: () => null
}));
vi.mock('Components/Common/Page', () => ({
    Page: (props: { children: ReactNode[] }) => { mocks.page(props); return props.children[1]; }
}));

type TableProps = DataPageProps<AllRecommendations, RecommendationDetails, { eventStore: string; namespace: string }>;

// Production withViewModel, observers, generated hooks, React effects and Arc consumer
// cleanup run for real; only transport and external presentation boundaries are stubbed.
export class a_recommendations_page {
    constructor() {
        beforeEach(async () => this.mount());
        afterEach(async () => {
            await this.root.unmount();
            this.cache.dispose();
            vi.restoreAllMocks();
            vi.unstubAllGlobals();
        });
        afterAll(() => {
            for (const module of ['react-router-dom', '@cratis/arc.react/dialogs', '@cratis/components/Notifications',
                '@cratis/components/DataPage', 'Components/Common/Page']) vi.doUnmock(module);
            vi.resetModules();
        });
    }

    readonly params = mocks.params;
    readonly activeQuery = AllRecommendations;
    readonly ignoredQuery = IgnoredRecommendations;
    cache!: QueryInstanceCache;
    root!: an_empty_root;
    recommendation!: RecommendationDetails;
    private operation: unknown;
    private complete!: (result: CommandResult) => void;

    get table() { return mocks.table.mock.lastCall![0] as TableProps; }
    get actions() {
        const groups = this.table.children as ReactElement<{ children: ReactElement<MenuItemProps>[] }>[];
        return groups[0].props.children.map(item => item.props);
    }
    get status() { return (mocks.page.mock.lastCall![0].children[0] as ReactElement<{ children: string }>).props.children; }
    get activeConsumer() { return this.cache.getDiagnosticsSnapshot().entries.find(entry => entry.queryName === new AllRecommendations().queryName)!; }
    get ignoredConsumer() { return this.cache.getDiagnosticsSnapshot().entries.find(entry => entry.queryName === new IgnoredRecommendations().queryName)!; }
    get activeSubscriptions() { return vi.mocked(AllRecommendations.prototype.subscribe).mock.calls; }
    get ignoredSubscriptions() { return vi.mocked(IgnoredRecommendations.prototype.subscribe).mock.calls; }
    get restoreCalls() { return vi.mocked(UnignoreRecommendation.prototype.execute).mock.calls; }

    async select() {
        await act(async () => { this.table.onSelectedItemsChange!([this.recommendation]); });
    }

    async toggle() {
        await act(async () => { this.executeAction(3); });
    }

    async startPerform() {
        vi.spyOn(PerformRecommendation.prototype, 'execute').mockReturnValue(new Promise(resolve => { this.complete = resolve; }));
        await act(async () => { this.operation = this.executeAction(0); });
    }

    async finishPerform() {
        await act(async () => { this.complete(CommandResult.empty); await this.operation; });
    }

    async restore() {
        mocks.confirm.mockResolvedValue(DialogResult.Yes);
        vi.spyOn(UnignoreRecommendation.prototype, 'execute').mockResolvedValue(CommandResult.empty);
        await act(async () => { await this.executeAction(2); });
    }

    private executeAction(index: number) {
        return this.actions[index].command!({
            selectionMode: 'multiple',
            selectedItems: Object.freeze([...(this.table.selectedItems ?? [])])
        });
    }

    private async mount() {
        vi.clearAllMocks();
        this.cache = new QueryInstanceCache();
        this.recommendation = Object.assign(new RecommendationDetails(), { id: Guid.parse('11111111-1111-1111-1111-111111111111'), name: 'Recommendation' });
        const subscribe = (callback: (result: QueryResult<RecommendationDetails[]>) => void) => {
            callback(QueryResult.empty([this.recommendation]));
            return { unsubscribe: vi.fn() } as unknown as ObservableQuerySubscription<RecommendationDetails[]>;
        };
        vi.spyOn(AllRecommendations.prototype, 'subscribe').mockImplementation(subscribe);
        vi.spyOn(IgnoredRecommendations.prototype, 'subscribe').mockImplementation(subscribe);
        this.root = new an_empty_root();
        await this.root.render(createElement(ArcContext.Provider, { value: { microservice: '', development: false } },
            createElement(QueryInstanceCacheContext.Provider, { value: this.cache },
                createElement(MemoryRouter, { initialEntries: [`/${this.params.eventStore}/${this.params.namespace}`] },
                    createElement(Routes, null, createElement(Route, { path: '/:eventStore/:namespace', element: createElement(Recommendations) }))))));
    }
}
