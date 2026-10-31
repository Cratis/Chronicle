// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { type ReactElement, type ReactNode } from 'react';
import { type DataPageProps, type MenuItemProps } from '@cratis/components/DataPage';
import { AllRecommendations, RecommendationDetails } from 'Features/Recommendations';
import { DialogResult } from '@cratis/arc.react/dialogs';
import { RecommendationsContent } from '../Recommendations';
import { a_view_model } from '../for_RecommendationsViewModel/when_managing_selection/given/a_view_model';

const mocks = vi.hoisted(() => ({ confirm: vi.fn(), success: vi.fn(), error: vi.fn(), effect: vi.fn() }));
vi.mock('react', async importOriginal => ({ ...await importOriginal<typeof import('react')>(), useEffect: mocks.effect }));
vi.mock('react-router-dom', () => ({ useParams: () => ({ eventStore: 'some-event-store', namespace: 'some-namespace' }) }));
vi.mock('@cratis/arc.react.mvvm', () => ({ withViewModel: (_model: unknown, component: unknown) => component }));
vi.mock('@cratis/arc.react/dialogs', () => ({ useConfirmationDialog: () => [mocks.confirm], DialogResult: { Yes: 0, No: 1 }, DialogButtons: { YesNo: 0 } }));
vi.mock('@cratis/components/Notifications', () => ({ toast: { success: mocks.success, error: mocks.error } }));
vi.mock('@cratis/components/DataPage', () => ({ DataPage: Object.assign(() => undefined, { MenuItems: () => undefined, Columns: () => undefined }), MenuItem: () => undefined, Column: () => undefined }));
vi.mock('Components/Common/Page', () => ({ Page: () => undefined }));

type TableProps = DataPageProps<AllRecommendations, RecommendationDetails, { eventStore: string; namespace: string }>;

describe('when using the controlled recommendation selection', () => {
    let context: a_view_model;
    let data: RecommendationDetails[];
    let state: { isReady: boolean; isSuccess: boolean; isAuthorized: boolean };
    const table = () => {
        const page = RecommendationsContent({ viewModel: context.viewModel, recommendations: { data, ...state }, query: AllRecommendations });
        return (page.props as { children: ReactElement<TableProps>[] }).children[1].props;
    };
    const actions = () => {
        const groups = table().children as ReactElement<{ children: ReactNode }>[];
        return (groups[0].props.children as ReactElement<MenuItemProps>[]).map(item => item.props);
    };
    beforeEach(() => {
        vi.clearAllMocks();
        context = new a_view_model();
        data = context.recommendations;
        state = { isReady: true, isSuccess: true, isAuthorized: true };
        mocks.confirm.mockResolvedValue(DialogResult.Yes);
    });
    afterEach(() => vi.restoreAllMocks());

    it('should bind row and checkbox selection to one controlled multiple selection', () => {
        const props = table();
        props.selectionMode!.should.equal('multiple');
        props.onSelectedItemsChange!(data);
        table().selectedItems!.should.deep.equal(data);
        actions().every(action => !action.disabled).should.be.true;
        props.onSelectedItemsChange!([]);
        actions().every(action => action.disabled).should.be.true;
    });

    it('should execute nothing when unselected', async () => {
        const perform = vi.spyOn(context.viewModel, 'performRecommendations');
        await actions()[0].command!();
        perform.mock.calls.should.have.lengthOf(0);
    });

    it('should cancel ignore without executing a command and release the guard', async () => {
        context.viewModel.selectedRecommendations = data;
        mocks.confirm.mockResolvedValue(DialogResult.No);
        const ignore = vi.spyOn(context.viewModel, 'ignoreRecommendations');
        await actions()[1].command!();
        ignore.mock.calls.should.have.lengthOf(0);
        context.viewModel.isBusy.should.be.false;
    });

    it('should capture before confirmation and block both actions while waiting', async () => {
        context.viewModel.selectedRecommendations = [data[0]];
        let confirm!: (result: DialogResult) => void;
        mocks.confirm.mockImplementation(() => new Promise<DialogResult>(resolve => { confirm = resolve; }));
        const ignore = vi.spyOn(context.viewModel, 'ignoreRecommendations').mockResolvedValue({ total: 1, failed: 0, failures: [] });
        const perform = vi.spyOn(context.viewModel, 'performRecommendations');
        const pending = actions()[1].command!();
        context.viewModel.selectedRecommendations = [data[1]];
        actions().every(action => action.disabled).should.be.true;
        await actions()[0].command!();
        await actions()[1].command!();
        confirm(DialogResult.Yes);
        await pending;
        ignore.mock.calls.should.deep.equal([[[data[0].id]]]);
        perform.mock.calls.should.have.lengthOf(0);
        mocks.success.mock.calls.should.have.lengthOf(1);
        context.viewModel.isBusy.should.be.false;
    });

    for (const ignore of [false, true]) {
        it('should show attempted and failed counts for partial and total failures', async () => {
            context.viewModel.selectedRecommendations = data;
            const action = vi.spyOn(context.viewModel, ignore ? 'ignoreRecommendations' : 'performRecommendations');
            for (const failed of [1, 2]) {
                action.mockResolvedValue({ total: 2, failed, failures: ['Denied'] });
                await actions()[ignore ? 1 : 0].command!();
                const message = mocks.error.mock.lastCall![0].description as string;
                message.should.contain(`${failed} failed of 2 attempted`);
                message.should.contain('Denied');
            }
            mocks.success.mock.calls.should.have.lengthOf(0);
        });
    }

    it('should report a rejected confirmation and release the in-flight guard', async () => {
        context.viewModel.selectedRecommendations = data;
        mocks.confirm.mockRejectedValue(new Error('dialog failed'));
        await actions()[1].command!();
        mocks.error.mock.lastCall![0].description.should.equal('dialog failed');
        context.viewModel.isBusy.should.be.false;
    });

    it('should prune removed rows and disable both actions after a settled successful empty snapshot', () => {
        context.viewModel.selectedRecommendations = data;
        data = [data[1]];
        table();
        (mocks.effect.mock.lastCall![0] as () => void)();
        context.viewModel.selectedRecommendations.should.deep.equal(data);
        data = [];
        table();
        (mocks.effect.mock.lastCall![0] as () => void)();
        context.viewModel.selectedRecommendations.should.have.lengthOf(0);
        actions().every(action => action.disabled).should.be.true;
    });

    for (const unavailable of ['isReady', 'isSuccess', 'isAuthorized'] as const) {
        it(`should retain selection when an empty snapshot is not ${unavailable}`, () => {
            const selected = data;
            context.viewModel.selectedRecommendations = selected;
            data = [];
            state[unavailable] = false;
            table();
            (mocks.effect.mock.lastCall![0] as () => void)();
            context.viewModel.selectedRecommendations.should.deep.equal(selected);
        });
    }
});
