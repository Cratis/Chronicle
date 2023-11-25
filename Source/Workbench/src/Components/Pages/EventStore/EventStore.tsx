// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { DefaultLayout } from "../../../Layout/Default/DefaultLayout";
import { Navigate, Route, Routes } from "react-router-dom";
import { Sequences } from "./Tenant/Sequences/Sequences";
import { IMenuItemGroup } from "../../../Layout/Default/Sidebar/MenuItem/MenuItem";
import * as mdIcons from 'react-icons/md';
import * as devIcons from 'react-icons/di';
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
                { label: 'Recommendations', url: 'tenant/:tenantId/recommendations', icon: mdIcons.MdOutlineLoupe },
                { label: 'Jobs', url: 'tenant/:tenantId/jobs', icon: mdIcons.MdOutlineLoupe },
                { label: 'Sequences', url: 'tenant/:tenantId/sequences', icon: mdIcons.MdDataArray },
                { label: 'Observers', url: 'tenant/:tenantId/observers', icon: mdIcons.MdMediation },
                { label: 'Failed Partitions', url: 'tenant/:tenantId/failed-partitions', icon: mdIcons.MdErrorOutline },
                { label: 'Identities', url: 'tenant/:tenantId/identities', icon: mdIcons.MdPeople },
            ]
        },
        {
            label: 'General',
            items: [
                { label: 'Types', url: 'types', icon: mdIcons.MdDataObject },
                { label: 'Projections', url: 'projections', icon: mdIcons.MdMediation },
                { label: 'Sequences', url: 'sequences', icon: mdIcons.MdDataArray },
                { label: 'Sinks', url: 'sinks', icon: devIcons.DiDatabase }
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
