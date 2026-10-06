import React from 'react';

const AccessDenied: React.FC = () => {
    return (
        <div>
            <header>
                <h1 className="text-danger">Access denied</h1>
                <p className="text-danger">You do not have access to this resource.</p>
            </header>
        </div>
    );
};

export default AccessDenied;