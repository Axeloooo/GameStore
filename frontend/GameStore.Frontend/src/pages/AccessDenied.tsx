import React from 'react';
import StatusAlert from '../components/StatusAlert';

const AccessDenied: React.FC = () => {
    return (
        <div>
            <header className="mt-4">
                <h1>Access denied</h1>
                <StatusAlert variant="danger">You do not have access to this page.</StatusAlert>
            </header>
        </div>
    );
};

export default AccessDenied;