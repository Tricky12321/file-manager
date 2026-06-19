import {Component} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {DialogService} from '../../dialog.service';

@Component({
  selector: 'confirm-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
})
export class ConfirmDialogComponent {
  constructor(public dialogService: DialogService) {
  }

  get selectedCount(): number {
    return this.dialogService.selectionRequest()?.items.filter(i => i.selected).length ?? 0;
  }
}
