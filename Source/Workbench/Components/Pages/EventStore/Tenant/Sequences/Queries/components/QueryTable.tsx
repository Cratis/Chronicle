import { DataTable } from 'primereact/datatable';
import { Column } from 'primereact/column';
import css from '../Queries.module.css';

export interface QueryTableProps {
    queryNumber: string;
}

export const QueryTable = (props: QueryTableProps) => {
    const { queryNumber } = props;

    return (
        <>
            <h1 className={css.tableHeading}>Sequence table</h1>
            <DataTable
                rows={100}
                paginator
                scrollable
                scrollHeight={'flex'}
                selectionMode='single'
                dataKey={queryNumber}
                filterDisplay='menu'
                emptyMessage='No data found'
            >
                <Column field='sequenceId' header='Id' sortable />
                <Column field='name' header='Name' sortable />
                <Column field='type' header='Sequence' sortable />
                <Column
                    field='nextEventSequenceNumber'
                    dataType='numeric'
                    header='Next event sequence number'
                    sortable
                />
                <Column field='runningState' header='State' sortable />
            </DataTable>
        </>
    );
};
