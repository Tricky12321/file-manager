import {Injectable, computed, signal} from '@angular/core';

/**
 * Tracks how many HTTP requests are currently in flight. Driven by loadingInterceptor;
 * the app shell shows a global progress bar whenever isLoading() is true.
 */
@Injectable({providedIn: 'root'})
export class LoadingService {
  private readonly count = signal(0);
  readonly isLoading = computed(() => this.count() > 0);

  start(): void {
    this.count.update(c => c + 1);
  }

  stop(): void {
    this.count.update(c => Math.max(0, c - 1));
  }
}
