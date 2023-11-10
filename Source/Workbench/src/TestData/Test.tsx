import { useLoaderData } from 'react-router-dom';
import { DefaultLayout } from '../Layout/Default/DefaultLayout';

export function Test() {
    const data = useLoaderData();

    return <DefaultLayout> {JSON.stringify(data)}</DefaultLayout>;
}
