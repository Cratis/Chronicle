import { Button } from 'primereact/button';
import { useNavigate } from 'react-router-dom';

export default function BasicButton() {
    const navigate = useNavigate();
    return (
        <div className='card flex justify-content-center'>
            <Button onClick={() => navigate('/test')} label='Test fetch data' />
        </div>
    );
}
