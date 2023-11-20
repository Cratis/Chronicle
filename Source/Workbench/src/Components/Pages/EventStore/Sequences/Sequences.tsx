import { Button } from "primereact/button"
import { NavLink, Navigate, Route, Routes } from "react-router-dom"
import { SequencesViewModel } from './SequencesViewModel'
import { observer } from 'mobx-react';
import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';

export interface SequencesProps {
    viewModel: SequencesViewModel;
}

export const Sequences = observer((props: SequencesProps) => {
    const { viewModel } = props;



    return <div>
        <h1>
            Sequences
        </h1>

        <Filters filters={viewModel.filters}
                 onAdded={viewModel.addFilter}
                 onChanged={viewModel.modifyFilter}
                 onRemoved={viewModel.removeFilter}/>

        <DataTable value={viewModel.events} >
            <Column field="metadata.sequenceNumber" header="#"/>
            <Column field="metadata.type.id" header="Type"/>
        </DataTable>

        {/*
        <NavLink to={'en'}>
            <Button color="primary">
                Side en
            </Button>

        </NavLink>
        <NavLink to={'to'}>
            <Button color="primary">
                Side to
            </Button>
        </NavLink>
        <div>
            <Routes>
                <Route path={''} element={<Navigate to={'en'} />} />
                <Route path={'en'} element={<div>Side 1</div>} />
                <Route path={'to'} element={<div>Side 2</div>} />
            </Routes>
        </div> */}
    </div>
})
