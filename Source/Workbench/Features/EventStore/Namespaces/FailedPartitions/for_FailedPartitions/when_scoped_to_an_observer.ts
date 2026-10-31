// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { type ReactElement } from 'react';
import { FailedPartitions } from '../FailedPartitions';
import { type FailedPartitionsViewModel } from '../FailedPartitionsViewModel';

const mocks = vi.hoisted(() => ({ search: '', setSearchParams: vi.fn() }));
vi.mock('react-router-dom', () => ({
    useParams: () => ({ eventStore: 'store', namespace: 'ns' }),
    useSearchParams: () => [new URLSearchParams(mocks.search), mocks.setSearchParams]
}));
vi.mock('@cratis/arc.react.mvvm', () => ({ withViewModel: (_model: unknown, component: unknown) => component }));
vi.mock('@cratis/components/DataPage', () => ({ DataPage: Object.assign(() => undefined, { MenuItems: () => undefined, Columns: () => undefined }), MenuItem: () => undefined, Column: () => undefined }));
vi.mock('Components/Common/Page', () => ({ Page: () => undefined }));

type Props = { children?: unknown; queryArguments?: { observerId?: string }; key?: string };

const render = () => {
    const page = (FailedPartitions as unknown as (props: { viewModel: FailedPartitionsViewModel }) => ReactElement)({ viewModel: { restorationState: 'idle' } as unknown as FailedPartitionsViewModel });
    const children = (page.props as { children: ReactElement<Props>[] }).children;
    const banner = children[0];
    const table = children[children.length - 1];
    return { banner: banner as unknown as ReactElement<{ children: ReactElement<{ onClick: () => void }>[] }> | false | undefined, table };
};

describe('when failed partitions are scoped to an observer', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        mocks.search = '';
    });

    it('should not narrow the query or show a banner without an observer in the url', () => {
        const { banner, table } = render();
        (table.props.queryArguments!.observerId === undefined).should.be.true;
        (!banner).should.be.true;
    });

    it('should narrow the query to the observer in the url', () => {
        mocks.search = '?observerId=My.Observer%2F1';
        render().table.props.queryArguments!.observerId!.should.equal('My.Observer/1');
    });

    it('should announce the narrowing with the observer id', () => {
        mocks.search = '?observerId=My.Observer';
        const { banner } = render();
        const text = (banner as unknown as ReactElement<{ children: ReactElement<{ children: string }>[] }>).props.children[0].props.children;
        text.should.contain('My.Observer');
    });

    it('should remove only the observer from the url when clearing the filter', () => {
        mocks.search = '?observerId=My.Observer&other=1';
        const { banner } = render();
        (banner as ReactElement<{ children: ReactElement<{ onClick: () => void }>[] }>).props.children[1].props.onClick();
        const next = mocks.setSearchParams.mock.calls[0][0] as URLSearchParams;
        next.toString().should.equal('other=1');
    });
});
