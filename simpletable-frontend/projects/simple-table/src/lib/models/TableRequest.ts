export interface TableRequest {
  pageNumber: number;
  pageSize: number;
  search?: string;
  sortColumn: string | null;
  sortColumnIndex: number | null;
  sortDirection?: 'asc' | 'desc';
}

