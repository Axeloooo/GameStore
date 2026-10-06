export class PaginationInfo {
  currentPage: number;
  totalPages: number;
  nameSearch?: string | null;

  constructor(currentPage: number, totalPages: number, nameSearch?: string | null) {
    this.currentPage = currentPage;
    this.totalPages = totalPages;
    this.nameSearch = nameSearch;
  }

  get hasPrevious(): boolean {
    return this.currentPage > 1;
  }

  get hasNext(): boolean {
    return this.currentPage < this.totalPages;
  }
}