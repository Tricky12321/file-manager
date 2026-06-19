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

export interface SelectableItem {
  path: string;
  selected: boolean;
}

export interface SelectionRequest {
  header: string;
  message: string;
  confirmButton: string;
  cancelButton: string;
  items: SelectableItem[];
  result$: Subject<string[] | null>;
}

export interface SelectionHandle {
  // Emits the list of still-selected paths on confirm, or null if cancelled.
  afterClosed(): Observable<string[] | null>;
}

@Injectable({
  providedIn: 'root'
})
export class DialogService {
  // The currently open confirm request (null = no dialog). The host component renders it.
  readonly request = signal<ConfirmRequest | null>(null);
  // The currently open selectable-delete request (null = none).
  readonly selectionRequest = signal<SelectionRequest | null>(null);

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

  // Opens a dialog listing files (pre-selected) so the user can deselect any to keep.
  openSelectionDialog(paths: string[], header = 'Confirm deletion',
                      message = 'Deselect any files you want to keep, then confirm.',
                      confirmButton = 'Delete selected', cancelButton = 'Cancel'): SelectionHandle {
    const result$ = new Subject<string[] | null>();
    this.selectionRequest.set({
      header,
      message,
      confirmButton,
      cancelButton,
      items: paths.map(path => ({path, selected: true})),
      result$
    });
    return {
      afterClosed: () => result$.asObservable()
    };
  }

  confirmSelection(): void {
    const req = this.selectionRequest();
    if (req) {
      req.result$.next(req.items.filter(i => i.selected).map(i => i.path));
      req.result$.complete();
    }
    this.selectionRequest.set(null);
  }

  cancelSelection(): void {
    const req = this.selectionRequest();
    if (req) {
      req.result$.next(null);
      req.result$.complete();
    }
    this.selectionRequest.set(null);
  }
}
