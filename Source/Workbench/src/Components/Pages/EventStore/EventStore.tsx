import { DefaultLayout } from "../../../Layout/Default/DefaultLayout";
import { Navigate, Route, Routes } from "react-router-dom";
import { Sequences } from "./Sequences/Sequences";
import {  IMenuItemGroup } from "../../../Layout/Default/Sidebar/MenuItem/MenuItem";
import {
    MdDataObject,
    MdErrorOutline,
    MdMediation,
    MdOutlineLoupe,
    MdOutlinePlayArrow,
    MdStream
} from "react-icons/md";
import { Types } from "./Types/Types";
import { Observers } from "./Observers/Observers";
import { Projections } from "./Projections/Projections";
import { FailedPartitions } from "./FailedPartitions/FailedPartitions";
import { ObserverReplayCandidates } from "./ObserverReplayCandidates/ObserverReplayCandidates";
export const EventStore = () => {
    const menuItems: IMenuItemGroup[] = [
        {
            items: [
                {label: 'Types', url: 'tenant/:tenantId/types', icon: MdDataObject},
                {label: 'Sequences', url: 'tenant/:tenantId/sequences', icon: MdStream},
                {label: 'Observers', url: 'tenant/:tenantId/observers', icon: MdMediation},
                {label: 'Projections', url: 'tenant/:tenantId/projections', icon: MdOutlinePlayArrow},
                {label: 'Failed Partitions', url: 'tenant/:tenantId/failed-partitions', icon: MdErrorOutline},
                {label: 'Observer Replay Candidates', url: 'tenant/:tenantId/observer-replay-candidates', icon: MdOutlineLoupe},
            ]
        },
        {
            label: 'Global stuff',
            items: [
                {label: 'Other things', url: 'test', icon: MdDataObject},
                {label: 'Foo', url: 'foo', icon: MdDataObject},
                {label: 'Bar', url: 'bar', icon: MdDataObject},
            ]
        }

    ];
    return (<>
        <Routes>
            <Route path=':eventStoreId'
                   element={<DefaultLayout leftMenuItems={menuItems}
                                           leftMenuBasePath={'/event-store/:eventStoreId'}/>}>

                <Route path={'tenant/:tenantId'}>
                    <Route path={''} element={<Navigate to={'types'}/>}/>
                    <Route path={'types'} element={<Types/>}/>
                    <Route path={'sequences/*'} element={<Sequences/>}/>
                    <Route path={'observers'} element={<Observers/>}/>
                    <Route path={'projections'} element={<Projections/>}/>
                    <Route path={'failed-partitions'} element={<FailedPartitions/>}/>
                    <Route path={'observer-replay-candidates'} element={<ObserverReplayCandidates/>}/>
                </Route>
                <Route path={'test'} element={<div>Test</div>}/>
                <Route path={'foo'} element={<div>Foo</div>}/>
                <Route path={'bar'} element={<div>Bar</div>}/>
            </Route>
        </Routes>
    </>)
}