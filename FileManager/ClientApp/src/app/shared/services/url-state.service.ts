import {Injectable} from '@angular/core';
import {Location} from '@angular/common';

/**
 * Reads/writes individual query-string keys on the current URL without triggering a
 * router navigation (uses Location.replaceState). Lets components persist UI state
 * (filters, search, sort, page) so a browser refresh restores it. Writes merge, so
 * different components can own different keys on the same URL.
 */
@Injectable({providedIn: 'root'})
export class UrlStateService {
  constructor(private location: Location) {
  }

  get(key: string): string | null {
    return new URLSearchParams(window.location.search).get(key);
  }

  // Merge the given keys into the current query string. null/undefined/'' removes a key.
  patch(updates: Record<string, string | number | boolean | null | undefined>): void {
    const params = new URLSearchParams(window.location.search);
    for (const [key, value] of Object.entries(updates)) {
      if (value === null || value === undefined || value === '') {
        params.delete(key);
      } else {
        params.set(key, String(value));
      }
    }
    const qs = params.toString();
    this.location.replaceState(window.location.pathname + (qs ? '?' + qs : ''));
  }
}
