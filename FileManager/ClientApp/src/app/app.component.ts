import {Component} from '@angular/core';
import {RouterOutlet, RouterLink, RouterLinkActive} from '@angular/router';
import {ConfirmDialogComponent} from './shared/services/dialogs/confirmDialog/confirm-dialog/confirm-dialog.component';
import {LoadingService} from './shared/services/loading.service';

@Component({
  selector: 'appRoot',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ConfirmDialogComponent],
  templateUrl: './app.component.html',
})
export class AppComponent {
  constructor(public loading: LoadingService) {
  }
}
