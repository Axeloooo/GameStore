import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import GamesClient from '../clients/GamesClient';
import { GameSummary } from '../models/GameSummary';
import { PaginationInfo } from '../models/PaginationInfo';
import Pagination from '../components/Pagination';
import StatusAlert from '../components/StatusAlert';
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
    const params: Record<string, string> = {};
    if (nameSearch) {
      params.name = nameSearch;
      params.page = '1';
    } else {
      params.page = '1';
    }
    setSearchParams(params);
  };

  return (
    <div>
      <section className="py-4">
        <p className="eyebrow mb-1">Fresh loot just landed</p>
        <h1 className="display-5">Find it. Grab it. Play it.</h1>
        <p className="lead text-body-secondary mb-0">Digital game codes, delivered to your order page after you pay.</p>
      </section>
      <div className="row mt-2">
        <div className="col-sm-4">
          <form id="searchGamesForm" method="post" className="d-flex" role="search" onSubmit={handleSearch}>
            <input
              className="form-control me-2"
              type="search"
              value={nameSearch || ''}
              onChange={(e) => setNameSearch(e.target.value)}
              placeholder="Search games"
              aria-label="Search games"
            />
            <button className="btn btn-outline-primary" type="submit">Search</button>
          </form>
        </div>
      </div>

      {error ? (
        <StatusAlert variant="danger" className="mt-3">We could not load the games. Please try again.</StatusAlert>
      ) : gamesPage === null || paginationInfo === null ? (
        <p className="mt-3"><em>Loading...</em></p>
      ) : (
        <>
          {gamesPage.data.length === 0 && (
            searchParams.get('name') ? (
              <p className="mt-4">No games match your search. Try a different name.</p>
            ) : (
              <p className="mt-4">The shelves are empty right now. Please check back soon.</p>
            )
          )}
          <div className="row row-cols-1 row-cols-md-5 mt-3">
            {gamesPage.data.map((game) => (
              <div key={game.id} className="col mt-4">
                <a href={`game/${game.id}`} className="card-link">
                  <div className="card h-100">
                    <div className="card-img-container">
                      {game.imageUri ? (
                        <img className="card-img-top" src={game.imageUri} alt={`Cover of ${game.name}`} />
                      ) : (
                        <div className="cover-placeholder card-img-top" role="img" aria-label={`No cover yet for ${game.name}`}>
                          {game.name.split(/\s+/).slice(0, 2).map((word) => word.charAt(0).toUpperCase()).join('')}
                        </div>
                      )}
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