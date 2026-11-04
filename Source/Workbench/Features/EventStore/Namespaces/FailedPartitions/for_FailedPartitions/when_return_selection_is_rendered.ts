// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { createElement, ReactNode } from 'react';
import { renderToStaticMarkup } from 'react-dom/server';
import { vi } from 'vitest';
import sinon from 'sinon';
import { Guid, JsonSerializer } from '@cratis/fundamentals';
import { FailedPartitionDetails as FailedPartition, GetFailedPartitions, RetryPartition } from 'Features/Observation';
import { FailedPartitionsViewModel } from '../FailedPartitionsViewModel';
import { FailedPartitions } from '../FailedPartitions';

const boundary = vi.hoisted(() => ({
    vm: undefined as FailedPartitionsViewModel | undefined,
    selection: undefined as FailedPartition | null | undefined,
    onSelectionChange: undefined as ((event: { value: FailedPartition | null }) => void) | undefined,
    command: undefined as (() => Promise<void>) | undefined
}));

vi.mock('@cratis/arc.react.mvvm', () => ({
    withViewModel: (type: unknown, render: (context: unknown) => ReactNode) => {
        void type;
        return () => render({ viewModel: boundary.vm, props: {} });
    }
}));
vi.mock('react-router-dom', () => ({
    useParams: () => ({ eventStore: 'store', namespace: 'namespace' }),
    useSearchParams: () => [new URLSearchParams(), vi.fn()]
}));
vi.mock('Components/Common/Page', () => ({ Page: ({ children }: { children: ReactNode }) => children }));
vi.mock('../FailedPartitionDetails', () => ({ FailedPartitionDetails: () => null }));

// Boundary adapter, not browser evidence: mirrors installed DataPage's controlled
// selection contract (null means no selection; undefined would be uncontrolled).
vi.mock('@cratis/components/DataPage', async () => {
    const { createElement } = await import('react');
    const DataPage = Object.assign((props: {
        selection?: FailedPartition | null;
        onSelectionChange: typeof boundary.onSelectionChange;
        children: ReactNode;
    }) => {
        boundary.selection = props.selection;
        boundary.onSelectionChange = props.onSelectionChange;
        return createElement('div', null,
            props.selection ? createElement('span', null, `row:${props.selection.id};details:${props.selection.id}`) : null,
            props.children);
    }, { MenuItems: ({ children }: { children: ReactNode }) => children, Columns: () => null });
    return { DataPage, Column: () => null, MenuItem: ({ command }: { command: () => Promise<void> }) => {
        boundary.command = command;
        return createElement('button', { disabled: !boundary.selection }, 'Retry');
    } };
});

afterAll(() => {
    vi.doUnmock('@cratis/arc.react.mvvm');
    vi.doUnmock('react-router-dom');
    vi.doUnmock('Components/Common/Page');
    vi.doUnmock('../FailedPartitionDetails');
    vi.doUnmock('@cratis/components/DataPage');
    vi.resetModules();
});

const origin = JsonSerializer.deserialize(FailedPartition, JSON.stringify({ id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', observerId: 'observer-a', partition: 'partition-a', attempts: [], isResolved: false }));
const other = JsonSerializer.deserialize(FailedPartition, JSON.stringify({ id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', observerId: 'observer-b', partition: 'partition-b', attempts: [], isResolved: false }));
const scope = { eventStore: 'store', namespace: 'namespace' };

for (const choice of [other, null]) {
    describe(`when return selection is rendered and the user ${choice ? 'selects B' : 'deselects'}`, () => {
        let markup: string;
        let restoredMarkup: string;
        let command: sinon.SinonStub;
        beforeEach(async () => {
            const query = sinon.createStubInstance(GetFailedPartitions);
            query.perform.onFirstCall().resolves({ isSuccess: true, data: [origin] } as never);
            let finish!: (value: never) => void;
            query.perform.onSecondCall().returns(new Promise(resolve => { finish = resolve; }));
            boundary.vm = new FailedPartitionsViewModel(scope, query);
            command = sinon.stub(RetryPartition.prototype, 'execute').resolves({} as never);
            await boundary.vm.handleQueryParams({ failedPartitionId: origin.id.toString() });
            restoredMarkup = renderToStaticMarkup(createElement(FailedPartitions));
            await boundary.vm.handleQueryParams({});
            const pending = boundary.vm.handleQueryParams({ failedPartitionId: origin.id.toString() });
            renderToStaticMarkup(createElement(FailedPartitions));
            boundary.onSelectionChange!({ value: choice });
            finish({ isSuccess: true, data: [origin] } as never);
            await pending;
            markup = renderToStaticMarkup(createElement(FailedPartitions));
            await boundary.command!();
        });
        afterEach(() => sinon.restore());
        it('should exercise transport-deserialized Guid identities', () => {
            origin.id.should.be.instanceOf(Guid);
            other.id.should.be.instanceOf(Guid);
        });
        it('should render the restored row and details through the page contract', () => restoredMarkup.should.contain(`row:${origin.id};details:${origin.id}`));
        it('should supply an explicit controlled selection including null', () => (boundary.selection === choice).should.be.true);
        it('should render the users choice after the late lookup', () => {
            if (choice) markup.should.contain(`row:${other.id};details:${other.id}`);
            else {
                markup.should.not.contain('details:');
                markup.should.contain('disabled');
            }
        });
        it('should keep Retry aligned with rendered selection', () => {
            command.callCount.should.equal(choice ? 1 : 0);
            if (choice) (command.firstCall.thisValue as RetryPartition).observerId.should.equal('observer-b');
        });
    });
}
