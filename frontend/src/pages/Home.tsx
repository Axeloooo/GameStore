import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import GamesClient from '../clients/GamesClient';
import { GameSummary } from '../models/GameSummary';
import { PaginationInfo } from '../models/PaginationInfo';
import Pagination from '../components/Pagination';
import "./Home.module.css";

const Home: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const [nameSearch, setNameSearch] = useState<string | null>(searchParams.get('name'));
  const [gamesPage, setGamesPage] = useState<{ data: GameSummary[] } | null>(null);
  const [paginationInfo, setPaginationInfo] = useState<PaginationInfo | null>(null);
  const [error, setError] = useState<string | null>(null);
  const client = new GamesClient();
  const pageSize = 10;

  useEffect(() => {
    const fetchGames = async () => {
      try {
        const pageNumber = parseInt(searchParams.get('page') || '1', 10);
        const name = searchParams.get('name') || '';
        const response = await client.getGamesAsync(pageNumber, pageSize, name);
        setGamesPage(response);
        setPaginationInfo(new PaginationInfo(pageNumber, response.totalPages, name));
        setError(null); // Clear any previous errors
      } catch (error: unknown) {
        if (error instanceof Error) {
          setError(error.message);
        } else {
          setError('An unknown error occurred');
        }
      }
    };
    fetchGames();
  }, [searchParams]);

  const handleSearch = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const params: any = {};
    if (nameSearch) {
      params.name = nameSearch;
      params.page = 1;
    } else {
      params.page = 1;
    }
    setSearchParams(params);
  };

  return (
    <div>
      <div className="row mt-2">
        <div className="col-sm-4">
          <form id="searchGamesForm" method="post" className="d-flex" role="search" onSubmit={handleSearch}>
            <input
              className="form-control me-2"
              type="search"
              value={nameSearch || ''}
              onChange={(e) => setNameSearch(e.target.value)}
              placeholder="Search store"
              aria-label="Search"
            />
            <button className="btn btn-outline-primary" type="submit">Search</button>
          </form>
        </div>
      </div>

      {error ? (
        <p className="mt-3 text-danger"><em>{error}</em></p>
      ) : gamesPage === null || paginationInfo === null ? (
        <p className="mt-3"><em>Loading...</em></p>
      ) : (
        <>
          <div className="row row-cols-1 row-cols-md-5 mt-3">
            {gamesPage.data.map((game) => (
              <div key={game.id} className="col mt-4">
                <a href={`game/${game.id}`} style={{ textDecoration: 'none' }}>
                  <div className="card h-100" style={{ transition: 'box-shadow 0.2s ease-in-out' }}>
                    <div className="card-img-container">
                      <img className="card-img-top" src={game.imageUri} alt="Card image cap" />
                    </div>
                    <div className="card-body">
                      <h5 className="card-title">{game.name}</h5>
                      <p className="card-text">${game.price}</p>
                    </div>
                  </div>
                </a>
              </div>
            ))}
          </div>
          <div className="row mt-2">
            <div className="col">
              <Pagination paginationInfo={paginationInfo} onPageChange={(pageNumber) => setSearchParams({ page: pageNumber.toString(), name: nameSearch || '' })} />
            </div>
          </div>
        </>
      )}
    </div>
  );
};

export default Home;