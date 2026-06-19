import {Injectable, signal} from '@angular/core';
import {Observable, Subject} from 'rxjs';

export interface ConfirmRequest {
  header: string;
  message: string;
  yesButton: string;
  noButton: string;
  result$: Subject<boolean>;
}

export interface ConfirmHandle {
  afterClosed(): Observable<boolean>;
}

@Injectable({
  providedIn: 'root'
})
export class DialogService {
  // The currently open confirm request (null = no dialog). The host component renders it.
  readonly request = signal<ConfirmRequest | null>(null);

  openConfirmDialog(msg: string, header = '', yesButton = 'Yes', noButton = 'Cancel'): ConfirmHandle {
    const result$ = new Subject<boolean>();
    this.request.set({header, message: msg, yesButton, noButton, result$});
    return {
      afterClosed: () => result$.asObservable()
    };
  }

  respond(result: boolean): void {
    const req = this.request();
    if (req) {
      req.result$.next(result);
      req.result$.complete();
    }
    this.request.set(null);
  }
}
