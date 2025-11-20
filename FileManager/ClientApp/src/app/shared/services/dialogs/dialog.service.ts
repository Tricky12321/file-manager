import {Injectable} from '@angular/core';
import {MatDialog} from '@angular/material/dialog';
import {ConfirmDialogComponent} from "./confirmDialog/confirm-dialog/confirm-dialog.component";

@Injectable({
  providedIn: 'root'
})

export class DialogService {
  constructor(private dialog: MatDialog) {
  }

  public modalOpen = false;

  openConfirmDialog(msg: any, header = '', yesButton = 'Yes', noButton = 'Cancel') {
    return this.dialog.open(ConfirmDialogComponent, {
      width: '500px',
      height: '200px',
      panelClass: 'confirm-dialog-container',
      disableClose: false,
      hasBackdrop: true,
      position: {top: '130px'},
      autoFocus: false,
      data: {
        header: header,
        message: msg,
        yesButton: yesButton,
        noButton: noButton
      }
    });
  }

}
