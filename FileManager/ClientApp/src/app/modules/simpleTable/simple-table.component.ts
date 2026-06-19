import {
  Component,
  Input,
  TemplateRef,
  AfterViewInit,
  ElementRef,
  Renderer2,
  EventEmitter, Output
} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpClient} from "@angular/common/http";
import {UrlStateService} from "../../shared/services/url-state.service";

export interface TableRequest {
  pageNumber: number;
  pageSize: number;
  search?: string;
  sortColumn: string | null;
  sortColumnIndex: number | null;
  sortDirection?: 'asc' | 'desc';
}

export interface TableResult<T> {
  items: T[];
  totalCount: number;
}

/**
 * Server-side data table. Headers are projected via <thead>; clicking a <th>
 * (optionally carrying a data-column-key) sorts via the backend. Search and
 * pagination post a TableRequest to [url] and render TableResult.items with the
 * provided row [template].
 */
@Component({
  selector: 'simple-table',
  templateUrl: './simple-table.component.html',
  styleUrls: ['simple-table.component.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule]
})
export class SimpleTableComponent implements AfterViewInit {
  @Input() data: any[] = [];
  @Input() template!: TemplateRef<any>;  // row template
  @Input() pageSize: number = 25;
  @Input() loadingText: string = 'Loading...';
  // URL that accepts a POST of TableRequest and returns TableResult<any>.
  @Input() url: string = "";
  @Output() urlChange: EventEmitter<string> = new EventEmitter<string>();
  @Input() pageSizeOptions: number[] = [25, 50, 100, 200, 500];

  public currentPage: number = 1;
  public totalPages: number = 1;

  searchTerm = '';
  isLoading = false;

  private currentSortAsc = true;

  public startIndex: number = 1;
  public endIndex: number = 1;
  public totalItemCount: number = 0;
  public sortColumnKey: string | null = null;
  public sortColumnIndex: number | null = null;

  // The page size the component was given, used to decide whether to persist size in the URL.
  private defaultPageSize: number = 25;

  constructor(
    private host: ElementRef<HTMLElement>,
    private renderer: Renderer2,
    public http: HttpClient,
    private urlState: UrlStateService
  ) {
  }

  ngAfterViewInit(): void {
    const table = this.host.nativeElement.querySelector('table');
    if (!table) {
      return;
    }

    this.renderer.addClass(table, 'simple-table');

    const thead = table.querySelector('thead');
    if (thead) {
      Array.from(thead.querySelectorAll('th')).forEach((th) => this.renderer.addClass(th, 'sortable'));
    }

    // Restore search / sort / page / size from the URL so a refresh keeps them.
    this.restoreFromUrl(thead);

    // Headers are projected content, so delegate clicks from the table to drive sorting.
    this.renderer.listen(table, 'click', (event: Event) => {
      const target = event.target as HTMLElement;
      const th = target?.closest('th');
      if (!th) {
        return;
      }

      const theadRow = th.parentElement;
      if (!theadRow) {
        return;
      }

      const ths = Array.from(theadRow.children).filter(
        (el) => el.tagName.toLowerCase() === 'th'
      ) as HTMLElement[];

      const colIndex = ths.indexOf(th);
      if (colIndex === this.sortColumnIndex) {
        this.currentSortAsc = !this.currentSortAsc;
      } else {
        this.currentSortAsc = true;
      }

      this.sortColumnKey = th.getAttribute('data-column-key') || colIndex.toString();
      this.sortColumnIndex = colIndex;

      ths.forEach((header, index) => {
        this.renderer.removeClass(header, 'sorted-asc');
        this.renderer.removeClass(header, 'sorted-desc');
        if (this.sortColumnIndex === index) {
          this.renderer.addClass(header, this.currentSortAsc ? 'sorted-asc' : 'sorted-desc');
        }
      });

      this.fetchServerData();
    });

    this.fetchServerData();
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;
    this.currentPage = 1;
    this.fetchServerData();
  }

  public update(url: string | null = null) {
    if (url != null) {
      this.url = url;
    }
    this.fetchServerData();
  }

  // Read persisted state from the URL and reflect it (including the sorted-column arrow).
  private restoreFromUrl(thead: HTMLElement | null): void {
    this.defaultPageSize = this.pageSize;

    const q = this.urlState.get('q');
    if (q) {
      this.searchTerm = q;
    }
    const size = parseInt(this.urlState.get('size') ?? '', 10);
    if (!isNaN(size) && size > 0) {
      this.pageSize = size;
    }
    const page = parseInt(this.urlState.get('page') ?? '', 10);
    if (!isNaN(page) && page > 0) {
      this.currentPage = page;
    }
    const sort = this.urlState.get('sort');
    if (sort) {
      this.sortColumnKey = sort;
      this.currentSortAsc = this.urlState.get('dir') !== 'desc';

      if (thead) {
        const ths = Array.from(thead.querySelectorAll('th')) as HTMLElement[];
        const idx = ths.findIndex((th, i) => (th.getAttribute('data-column-key') || i.toString()) === sort);
        if (idx >= 0) {
          this.sortColumnIndex = idx;
          this.renderer.addClass(ths[idx], this.currentSortAsc ? 'sorted-asc' : 'sorted-desc');
        }
      }
    }
  }

  // Mirror current search / sort / page / size into the URL (no navigation).
  private syncUrl(): void {
    this.urlState.patch({
      q: this.searchTerm || null,
      page: this.currentPage > 1 ? this.currentPage : null,
      size: this.pageSize !== this.defaultPageSize ? this.pageSize : null,
      sort: this.sortColumnKey,
      dir: this.sortColumnKey ? (this.currentSortAsc ? 'asc' : 'desc') : null,
    });
  }

  private fetchServerData(): void {
    if (!this.url) {
      return;
    }
    this.syncUrl();
    this.isLoading = true;
    const request: TableRequest = {
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchTerm || undefined,
      sortColumn: this.sortColumnKey,
      sortColumnIndex: this.sortColumnIndex,
      sortDirection: this.currentSortAsc ? 'asc' : 'desc'
    };
    this.http.post<TableResult<any>>(this.url, request).subscribe({
      next: (result: TableResult<any>) => {
        this.data = result.items;
        this.totalItemCount = result.totalCount;
        this.totalPages = Math.max(1, Math.ceil(result.totalCount / this.pageSize));
        this.startIndex = (this.currentPage - 1) * this.pageSize + 1;
        this.endIndex = Math.min(this.startIndex - 1 + this.pageSize, this.totalItemCount);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  onPageSizeChange() {
    this.currentPage = 1;
    this.fetchServerData();
  }

  goToPreviousPage() {
    this.currentPage = this.currentPage - 1;
    this.fetchServerData();
  }

  goToNextPage() {
    this.currentPage = this.currentPage + 1;
    this.fetchServerData();
  }

  goToPage(page: number) {
    this.currentPage = page;
    this.fetchServerData();
  }

  getPageNumbers() {
    const startPage = Math.max(1, this.currentPage - 2);
    const endPage = Math.min(this.totalPages, this.currentPage + 2);
    return Array.from({length: Math.max(0, endPage - startPage + 1)}, (_, i) => startPage + i);
  }
}
