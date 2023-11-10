import { DefaultLayout } from "../../../Layout/Default/DefaultLayout";
import { Navigate, Route, Routes } from "react-router-dom";
import { Sequences } from "./Sequences/Sequences";
import { IMenuItem } from "../../../Layout/Default/Sidebar/MenuItem/MenuItem";
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
    const menuItems: IMenuItem[] = [
        {
            label: 'Types',
            url: 'types',
            icon: MdDataObject

        },
        {
            label: 'Sequences',
            url: 'sequences',
            icon: MdStream

        },
        {
            label: 'Observers',
            url: 'observers',
            icon: MdErrorOutline


        },
        {
            label: 'Projections',
            url: 'projections',
            icon: MdOutlineLoupe
        },
        {
            label: 'Failed Partitions',
            url: 'failed-partitions',
            icon: MdMediation
        },
        {
            label: 'Observer replay candidates',
            url: 'observer-replay-candidates',
            icon: MdOutlinePlayArrow
        },
    ];
    return (<>
        <Routes>
            <Route path=':eventStoreId'
                   element={<DefaultLayout leftMenuItems={menuItems}
                                           leftMenuBasePath={'/event-store/:eventStoreId'}/>}>
                <Route path='' element={<Navigate to={'types'}/>}/>
                <Route path={'types'} element={<Types/>}/>
                <Route path={'sequences/*'} element={<Sequences/>}/>
                <Route path={'observers'} element={<Observers/>}/>
                <Route path={'projections'} element={<Projections/>}/>
                <Route path={'failed-partitions'} element={<FailedPartitions/>}/>
                <Route path={'observer-replay-candidates'} element={<ObserverReplayCandidates/>}/>
            </Route>
        </Routes>
    </>)
}