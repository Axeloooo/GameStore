// src/components/Pagination.tsx
import React from 'react';
import { PaginationInfo } from '../models/PaginationInfo';
import { useNavigate } from 'react-router-dom';

interface PaginationProps {
  paginationInfo: PaginationInfo;
  onPageChange: (pageNumber: number) => void;
}

const Pagination: React.FC<PaginationProps> = ({ paginationInfo, onPageChange }) => {
  const navigate = useNavigate();

  const getPageNumbers = (paginationInfo: PaginationInfo) => {
    const pageNumbers = [];
    for (let i = 1; i <= paginationInfo.totalPages; i++) {
      pageNumbers.push(i);
    }
    return pageNumbers;
  };

  const handlePageChange = (pageNumber: number) => {
    onPageChange(pageNumber);
    navigate(`?page=${pageNumber}`);
  };

  return (
    <nav>
      <ul className="pagination justify-content-center">
        <li className={`page-item ${!paginationInfo.hasPrevious ? 'disabled' : ''}`}>
          <button className="page-link" onClick={() => handlePageChange(paginationInfo.currentPage - 1)} disabled={!paginationInfo.hasPrevious}>
            Previous
          </button>
        </li>

        {getPageNumbers(paginationInfo).map((pageNumber) => (
          <li key={pageNumber} className={`page-item ${pageNumber === paginationInfo.currentPage ? 'active' : ''}`}>
            <button className="page-link" onClick={() => handlePageChange(pageNumber)}>
              {pageNumber}
            </button>
          </li>
        ))}

        <li className={`page-item ${!paginationInfo.hasNext ? 'disabled' : ''}`}>
          <button className="page-link" onClick={() => handlePageChange(paginationInfo.currentPage + 1)} disabled={!paginationInfo.hasNext}>
            Next
          </button>
        </li>
      </ul>
    </nav>
  );
};

export default Pagination;