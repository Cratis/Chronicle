// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DefaultLayout } from "../../../Layout/Default/DefaultLayout";
import { Navigate, Route, Routes } from "react-router-dom";
import { Sequences } from "./Sequences/Sequences";
import { IMenuItemGroup } from "../../../Layout/Default/Sidebar/MenuItem/MenuItem";
import * as icons from 'react-icons/md';
import { Types } from "./Types/Types";
import { Observers } from "./Observers/Observers";
import { Projections } from "./Projections/Projections";
import { FailedPartitions } from "./FailedPartitions/FailedPartitions";
import { ObserverReplayCandidates } from "./ObserverReplayCandidates/ObserverReplayCandidates";

export const EventStore = () => {
    const menuItems: IMenuItemGroup[] = [
        {
            items: [
                { label: 'Recommendations', url: 'tenant/:tenantId/recommendations', icon: icons.MdOutlineLoupe },
                { label: 'Types', url: 'tenant/:tenantId/types', icon: icons.MdDataObject },
                { label: 'Sequences', url: 'tenant/:tenantId/sequences', icon: icons.MdStream },
                { label: 'Observers', url: 'tenant/:tenantId/observers', icon: icons.MdMediation },
                { label: 'Projections', url: 'tenant/:tenantId/projections', icon: icons.MdOutlinePlayArrow },
                { label: 'Failed Partitions', url: 'tenant/:tenantId/failed-partitions', icon: icons.MdErrorOutline },
            ]
        },
        {
            label: 'Global stuff',
            items: [
                { label: 'Other things', url: 'test', icon: icons.MdDataObject },
                { label: 'Foo', url: 'foo', icon: icons.MdDataObject },
                { label: 'Bar', url: 'bar', icon: icons.MdDataObject },
            ]
        }
    ];
    return (<>
        <Routes>
            <Route path=':eventStoreId'
                element={<DefaultLayout leftMenuItems={menuItems} leftMenuBasePath={'/event-store/:eventStoreId'} />}>

                <Route path={'tenant/:tenantId'}>
                    <Route path={''} element={<Navigate to={'types'} />} />
                    <Route path={'types'} element={<Types />} />
                    <Route path={'sequences/*'} element={<Sequences />} />
                    <Route path={'observers'} element={<Observers />} />
                    <Route path={'projections'} element={<Projections />} />
                    <Route path={'failed-partitions'} element={<FailedPartitions />} />
                    <Route path={'observer-replay-candidates'} element={<ObserverReplayCandidates />} />
                </Route>
                <Route path={'test'} element={<div>Test</div>} />
                <Route path={'foo'} element={<div>Foo</div>} />
                <Route path={'bar'} element={<div>Bar</div>} />
            </Route>
        </Routes>
    </>)
}
