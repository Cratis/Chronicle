// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DefaultLayout } from "../../../Layout/Default/DefaultLayout";
import { Navigate, Route, Routes } from "react-router-dom";
import { Sequences } from "./Tenant/Sequences/Sequences";
import { IMenuItemGroup } from "../../../Layout/Default/Sidebar/MenuItem/MenuItem";
import * as icons from 'react-icons/md';
import { Types } from "./General/Types/Types";
import { Observers } from "./Tenant/Observers/Observers";
import { Projections } from "./General/Projections/Projections";
import { FailedPartitions } from "./Tenant/FailedPartitions/FailedPartitions";
import { Recommendations } from "./Tenant/Recommendations/Recommendations";
import { Jobs } from './Tenant/Jobs/Jobs';
import { Identities } from './Tenant/Identities/Identities';
import { Sequences as GeneralSequences } from './General/Sequences/Sequences';
import { Sinks } from './General/Sinks/Sinks';

export const EventStore = () => {
    const menuItems: IMenuItemGroup[] = [
        {
            items: [
                { label: 'Recommendations', url: 'tenant/:tenantId/recommendations', icon: icons.MdOutlineLoupe },
                { label: 'Jobs', url: 'tenant/:tenantId/jobs', icon: icons.MdOutlineLoupe },
                { label: 'Sequences', url: 'tenant/:tenantId/sequences', icon: icons.MdStream },
                { label: 'Observers', url: 'tenant/:tenantId/observers', icon: icons.MdMediation },
                { label: 'Failed Partitions', url: 'tenant/:tenantId/failed-partitions', icon: icons.MdErrorOutline },
                { label: 'Identities', url: 'tenant/:tenantId/identities', icon: icons.MdErrorOutline },
            ]
        },
        {
            label: 'General',
            items: [
                { label: 'Types', url: 'types', icon: icons.MdDataObject },
                { label: 'Projections', url: 'projections', icon: icons.MdOutlinePlayArrow },
                { label: 'Sequences', url: 'sequences', icon: icons.MdStream },
                { label: 'Sinks', url: 'sinks', icon: icons.MdStream }
            ]
        }
    ];
    return (<>
        <Routes>
            <Route path=':eventStoreId'
                element={<DefaultLayout leftMenuItems={menuItems} leftMenuBasePath={'/event-store/:eventStoreId'}/>}>

                <Route path={'tenant/:tenantId'}>
                    <Route path={''} element={<Navigate to={'recommendations'} />} />
                    <Route path={'recommendations'} element={<Recommendations />} />
                    <Route path={'jobs'} element={<Jobs />} />
                    <Route path={'sequences/*'} element={<Sequences />} />
                    <Route path={'observers'} element={<Observers />} />
                    <Route path={'failed-partitions'} element={<FailedPartitions />} />
                    <Route path={'identities'} element={<Identities />} />
                </Route>
                <Route path={'types'} element={<Types />} errorElement={<Projections/>}/>
                <Route path={'projections'} element={<Projections />} />
                <Route path={'sequences'} element={<GeneralSequences />} />
                <Route path={'sinks'} element={<Sinks />} />
            </Route>
        </Routes>
    </>)
}
