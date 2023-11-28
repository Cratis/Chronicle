import { Inplace, InplaceContent, InplaceDisplay } from 'primereact/inplace';
import { InputText } from 'primereact/inputtext';
import { ChangeEvent } from 'react';

export interface QueryType {
    title: string;
    id: string;
}

export interface QueryHeaderProps {
    idx: number;
    query: QueryType;
    onQueryChange: (e: ChangeEvent<HTMLInputElement>, idx: number) => void;
}

export const QueryHeader = (props: QueryHeaderProps) => {
    const { query, idx, onQueryChange } = props;
    return (
        <Inplace>
            <InplaceDisplay>{query.title}</InplaceDisplay>
            <InplaceContent>
                <InputText value={query.title} onChange={(e) => onQueryChange(e, idx)} />
            </InplaceContent>
        </Inplace>
    );
};
