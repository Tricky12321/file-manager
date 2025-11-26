import {Component, ElementRef, EventEmitter, Input, Output, Renderer2, TemplateRef, ViewChild} from '@angular/core';
import {HttpClient} from '@angular/common/http';

@Component({
  selector: 'simple-table',
  imports: [],
  templateUrl: './simple-table.component.html',
  styleUrls: ['simple-table.component.scss'],
})
export class SimpleTableComponent {
  @Input() data: any[] = [];             // client-side data
  @Input() serverSide = false;           // toggle client/server mode
  @Input() template!: TemplateRef<any>;  // row template
  @Input() pageSize: number = 10;
  @Input() loadingText: string = 'Loading...';
  // Should be the URL to post TableRequest to and get TableResult<any> from
  // It should update when urlChanged is triggered
  @Input() url: string = "";
  @Output() urlChange: EventEmitter<string> = new EventEmitter<string>();
  @Input() pageSizeOptions: number[] = [10, 25, 50, 100];

  // I need a function that can be called to refresh data from the calling component, e.g. after an action
  public currentPage: number = 1;
  public totalPages: number = 1;
  /**
   * Function for server-side data loading.
   * It receives a TableRequest and returns an observable of TableResult<any>.
   * Example: (req) => this.personService.getPersons(req)
   */

  searchTerm = '';
  isLoading = false;

  private currentSortIndex: number | null = null;
  private currentSortAsc = true;

  @ViewChild('searchInput', {static: false}) searchInput: ElementRef<HTMLInputElement> | undefined;
  public startIndex: number = 1;
  public endIndex: number = 1;
  public totalItemCount: number = 0;
  public sortColumnKey: string | null = null;
  public sortColumnIndex: number | null = null;

  constructor(
    private host: ElementRef<HTMLElement>,
    private renderer: Renderer2,
    public http: HttpClient
  ) {
  }

  ngAfterViewInit(): void {
    const table = this.host.nativeElement.querySelector('table');
    if (!table) {
      return;
    }

    // add class sortable to all th columns
    const thead = table.querySelector('thead');
    if (thead) {
      const ths = Array.from(thead.querySelectorAll('th'));
      ths.forEach((th) => {
        this.renderer.addClass(th, 'sortable');
      });
    }

    // Add class simple-dashboard to the table
    this.renderer.addClass(table, 'simple-table');

    // Delegate click events from the table to catch TH clicks for sorting
    this.renderer.listen(table, 'click', (event: Event) => {
      const target = event.target as HTMLElement;
      if (!target) return;

      const th = target.closest('th');
      if (!th) {
        return;
      }

      const theadRow = th.parentElement;
      if (!theadRow) return;

      const ths = Array.from(theadRow.children).filter(
        (el) => el.tagName.toLowerCase() === 'th'
      ) as HTMLElement[];

      const colIndex = ths.indexOf(th);
      if (colIndex == this.sortColumnIndex) {
        this.currentSortAsc = !this.currentSortAsc;
      }
      const sortColumnKey = th.getAttribute('data-column-key') || colIndex.toString();
      if (sortColumnKey != null) {
        this.sortColumnKey = sortColumnKey;
        this.sortColumnIndex = colIndex;
      }
      // add class sorted-asc/sorted-desc to the sorted column, remove from others
      ths.forEach((header, index) => {
        this.renderer.removeClass(header, 'sorted-asc');
        this.renderer.removeClass(header, 'sorted-desc');
        if (this.sortColumnIndex == index) {
          if (this.currentSortAsc) {
            this.renderer.addClass(header, 'sorted-asc');
          } else {
            this.renderer.addClass(header, 'sorted-desc');
          }
        }
      });


      if (this.serverSide) {
        this.fetchServerData();
      } else {
        this.sortByColumn(colIndex, sortColumnKey);
      }

    });

    // Initial server-side load if applicable
    if (this.serverSide) {
      this.fetchServerData();
    }
  }

  onSearchChange(value: string): void {
    this.searchTerm = value;

    if (this.serverSide) {
      // Delegate to backend
      this.fetchServerData();
    } else {
      // Client-side filtering on DOM
      this.applyClientSideFilter();
    }
  }

  public update(url: string | null = null) {
    if (url != null) {
      this.url = url;
    }
    if (this.serverSide) {
      this.fetchServerData();
    }
  }

  private fetchServerData(): void {
    this.isLoading = true;
    const request: TableRequest = {
      pageNumber: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchTerm || undefined,
      sortColumn: this.sortColumnKey,
      sortColumnIndex: this.sortColumnIndex,
      sortDirection: this.currentSortAsc ? 'asc' : 'desc'
    };
    // We don't know the concrete type here, but we expect it to be an RxJS observable
    this.http.post<TableResult<any>>(this.url, request).subscribe({
      next: (result: TableResult<any>) => {
        this.data = result.items;
        this.totalItemCount = result.totalCount;
        this.totalPages = Math.ceil(result.totalCount / this.pageSize);
        this.startIndex = (this.currentPage - 1) * this.pageSize + 1;
        this.endIndex = (Number(this.startIndex) - 1) + Number(this.pageSize);
        this.isLoading = false;
        // After data change, re-apply client-side filter (no-op for server mode)
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  private sortByColumn(index: number, sortColumnKey: string): void {
    const table = this.host.nativeElement.querySelector('table');
    if (!table) {
      return;
    }

    const tbody = table.querySelector('tbody');
    if (!tbody) {
      return;
    }

    // Toggle sort direction if same column is clicked again
    if (this.currentSortIndex === index) {
      this.currentSortAsc = !this.currentSortAsc;
    } else {
      this.currentSortIndex = index;
      this.currentSortAsc = true;
    }

    const direction = this.currentSortAsc ? 1 : -1;

    if (this.serverSide) {
      // Delegate sorting to backend
      this.fetchServerData();
      return;
    }

    // CLIENT-SIDE sorting (DOM-based)
    const rows = Array.from(tbody.querySelectorAll('tr'));
    if (rows.length === 0) return;

    const sorted = rows.sort((a, b) => {
      const cellA = a.children[index] as HTMLElement | undefined;
      const cellB = b.children[index] as HTMLElement | undefined;

      const valA = this.getCellSortValue(cellA);
      const valB = this.getCellSortValue(cellB);

      const numA = parseFloat(valA);
      const numB = parseFloat(valB);

      const bothNumeric = !isNaN(numA) && !isNaN(numB);

      if (bothNumeric) {
        if (numA < numB) return -1 * direction;
        if (numA > numB) return 1 * direction;
        return 0;
      }
      return valA.localeCompare(valB, undefined, {numeric: true}) * direction;
    });

    sorted.forEach((row) => tbody.appendChild(row));

    // Re-apply filter after sorting
    this.applyClientSideFilter();
  }

  private getCellSortValue(cell?: HTMLElement): string {
    if (!cell) return '';

    const attrVal = cell.getAttribute('data-sort');
    if (attrVal != null && attrVal.trim() !== '') {
      return attrVal.trim();
    }

    return (cell.textContent || '').trim();
  }

  private applyClientSideFilter(): void {
    const term = (this.searchTerm || '').trim().toLowerCase();
    const table = this.host.nativeElement.querySelector('table');
    if (!table) return;

    const tbody = table.querySelector('tbody');
    if (!tbody) return;

    const rows = Array.from(tbody.querySelectorAll('tr'));
    if (rows.length === 0) return;

    if (!term) {
      // Reset visibility
      rows.forEach((row) => {
        (row as HTMLElement).style.display = '';
      });
      return;
    }

    rows.forEach((row) => {
      const cells = Array.from(row.children) as HTMLElement[];
      const match = cells.some((cell) => {
        const searchAttr = cell.getAttribute('data-search');
        const value = (searchAttr != null && searchAttr.trim() !== '')
          ? searchAttr
          : (cell.textContent || '');
        return value.toLowerCase().includes(term);
      });

      (row as HTMLElement).style.display = match ? '' : 'none';
    });
  }

  onPageSizeChange() {
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
    // Return an array of page numbers for pagination, starting from currentPage +1 to currentPage +5
    const pages: number[] = [];
    const startPage = Math.max(1, this.currentPage - 2);
    const endPage = Math.min(this.totalPages, this.currentPage + 2);
    return Array.from({length: endPage - startPage + 1}, (_, i) => startPage + i);
  }
}
