import {Component} from '@angular/core';
import {SwUpdate, VersionEvent} from "@angular/service-worker";

@Component({
    selector: 'appRoot',
    templateUrl: './app.component.html',
    standalone: false
})
export class AppComponent {
  title = 'app';

    constructor(update: SwUpdate) {
        if (!update.isEnabled) {
            return;
        }
      update.versionUpdates.subscribe((event: VersionEvent) => {
        if (!document.hidden && event.type == 'VERSION_DETECTED') {
          if (confirm('New update available, please reload page')) {
            update.activateUpdate().then((newUpdate) => {
              window.location.reload();
            });
          }
        }
      });
    }
}
