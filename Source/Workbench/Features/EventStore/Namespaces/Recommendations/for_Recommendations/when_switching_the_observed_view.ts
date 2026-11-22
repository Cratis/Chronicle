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
import { Recommendations } from '../Recommendations';
import { an_empty_root } from './given/an_empty_root';

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
const table = () => mocks.table.mock.lastCall![0] as TableProps;
const actions = () => {
    const groups = table().children as ReactElement<{ children: ReactElement<MenuItemProps>[] }>[];
    return groups[0].props.children.map(item => item.props);
};
const status = () => (mocks.page.mock.lastCall![0].children[0] as ReactElement<{ children: string }>).props.children;

// Only stub the transport boundary: production withViewModel, observers, generated hooks,
// React effects and Arc cache reference/listener cleanup all run for real.
describe('when switching the observed recommendation view', () => {
    let root: an_empty_root;
    let cache: QueryInstanceCache;
    let recommendation: RecommendationDetails;
    const consumers = (queryName: string) => cache.getDiagnosticsSnapshot().entries.find(entry => entry.queryName === queryName);
    const activeName = new AllRecommendations().queryName;
    const ignoredName = new IgnoredRecommendations().queryName;

    beforeEach(async () => {
        vi.clearAllMocks();
        cache = new QueryInstanceCache();
        recommendation = Object.assign(new RecommendationDetails(), { id: Guid.parse('11111111-1111-1111-1111-111111111111'), name: 'Recommendation' });
        const subscribe = (callback: (result: QueryResult<RecommendationDetails[]>) => void) => {
            callback(QueryResult.empty([recommendation]));
            return { unsubscribe: vi.fn() } as unknown as ObservableQuerySubscription<RecommendationDetails[]>;
        };
        vi.spyOn(AllRecommendations.prototype, 'subscribe').mockImplementation(subscribe);
        vi.spyOn(IgnoredRecommendations.prototype, 'subscribe').mockImplementation(subscribe);
        root = new an_empty_root();
        await root.render(createElement(ArcContext.Provider, { value: { microservice: '', development: false } },
            createElement(QueryInstanceCacheContext.Provider, { value: cache },
                createElement(MemoryRouter, { initialEntries: [`/${mocks.params.eventStore}/${mocks.params.namespace}`] },
                    createElement(Routes, null, createElement(Route, { path: '/:eventStore/:namespace', element: createElement(Recommendations) }))))));
    });
    afterEach(async () => {
        await root.unmount();
        cache.dispose();
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });
    afterAll(() => {
        for (const module of ['react-router-dom', '@cratis/arc.react/dialogs', '@cratis/components/Notifications',
            '@cratis/components/DataPage', 'Components/Common/Page']) vi.doUnmock(module);
        vi.resetModules();
    });

    it('should initially register only the active query with the current route arguments', () => {
        consumers(activeName)!.subscriberCount.should.equal(1);
        consumers(activeName)!.listenerCount.should.equal(1);
        cache.getDiagnosticsSnapshot().entries.should.have.lengthOf(1);
        vi.mocked(IgnoredRecommendations.prototype.subscribe).mock.calls.should.have.lengthOf(0);
        vi.mocked(AllRecommendations.prototype.subscribe).mock.calls[0][1]!.should.deep.equal(mocks.params);
        table().query.should.equal(AllRecommendations);
        table().queryArguments!.should.deep.equal(mocks.params);
    });

    it('should release the active consumer and register only the ignored consumer on toggle', async () => {
        await act(async () => { table().onSelectedItemsChange!([recommendation]); });
        actions()[0].disabled!.should.be.false;
        await act(async () => { actions()[3].command!(); });
        consumers(activeName)!.subscriberCount.should.equal(0);
        consumers(activeName)!.listenerCount.should.equal(0);
        // Retention may keep the transport alive; it must not retain a mounted consumer.
        consumers(activeName)!.subscribed.should.be.true;
        consumers(ignoredName)!.subscriberCount.should.equal(1);
        consumers(ignoredName)!.listenerCount.should.equal(1);
        vi.mocked(IgnoredRecommendations.prototype.subscribe).mock.calls[0][1]!.should.deep.equal(mocks.params);
        table().query.should.equal(IgnoredRecommendations);
        table().selectedItems!.should.have.lengthOf(0);
    });

    it('should not accumulate consumers or retain the old selection when toggling back', async () => {
        await act(async () => { actions()[3].command!(); });
        await act(async () => { table().onSelectedItemsChange!([recommendation]); });
        actions()[2].disabled!.should.be.false;
        await act(async () => { actions()[3].command!(); });
        consumers(activeName)!.subscriberCount.should.equal(1);
        consumers(activeName)!.listenerCount.should.equal(1);
        consumers(ignoredName)!.subscriberCount.should.equal(0);
        consumers(ignoredName)!.listenerCount.should.equal(0);
        vi.mocked(AllRecommendations.prototype.subscribe).mock.calls.should.have.lengthOf(1);
        table().query.should.equal(AllRecommendations);
        table().selectedItems!.should.have.lengthOf(0);
    });

    it('should keep selection and busy state reactive inside the active observer', async () => {
        let complete!: (result: CommandResult) => void;
        vi.spyOn(PerformRecommendation.prototype, 'execute').mockReturnValue(new Promise(resolve => { complete = resolve; }));
        await act(async () => { table().onSelectedItemsChange!([recommendation]); });
        actions()[0].disabled!.should.be.false;
        let operation: unknown;
        await act(async () => { operation = actions()[0].command!(); });
        status().should.not.equal('');
        actions().every(action => action.disabled).should.be.true;
        await act(async () => { complete(CommandResult.empty); await operation; });
        status().should.equal('');
        table().selectedItems!.should.have.lengthOf(0);
        actions()[3].disabled!.should.be.false;
    });

    it('should keep the ignored selection and restore action reactive', async () => {
        mocks.confirm.mockResolvedValue(DialogResult.Yes);
        const restore = vi.spyOn(UnignoreRecommendation.prototype, 'execute').mockResolvedValue(CommandResult.empty);
        await act(async () => { actions()[3].command!(); });
        await act(async () => { table().onSelectedItemsChange!([recommendation]); });
        actions()[2].disabled!.should.be.false;
        actions()[0].disabled!.should.be.true;
        await act(async () => { await actions()[2].command!(); });
        restore.mock.calls.should.have.lengthOf(1);
        table().selectedItems!.should.have.lengthOf(0);
        actions()[2].disabled!.should.be.true;
        status().should.equal('');
    });

    it('should release every consumer when the page unmounts', async () => {
        await root.render(null);
        consumers(activeName)!.subscriberCount.should.equal(0);
        consumers(activeName)!.listenerCount.should.equal(0);
    });
});
