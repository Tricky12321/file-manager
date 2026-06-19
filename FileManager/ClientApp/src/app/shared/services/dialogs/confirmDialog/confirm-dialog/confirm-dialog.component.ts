import {Component} from '@angular/core';
import {DialogService} from '../../dialog.service';

@Component({
  selector: 'confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
})
export class ConfirmDialogComponent {
  constructor(public dialogService: DialogService) {
  }
}
