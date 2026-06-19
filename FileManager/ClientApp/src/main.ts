import {enableProdMode, importProvidersFrom} from '@angular/core';
import {bootstrapApplication} from '@angular/platform-browser';
import {provideRouter, withRouterConfig} from '@angular/router';
import {provideHttpClient, withInterceptors} from '@angular/common/http';
import {loadingInterceptor} from './app/shared/interceptors/loading.interceptor';
import {provideAnimations} from '@angular/platform-browser/animations';
import {ToastrModule} from 'ngx-toastr';
import {registerLocaleData} from '@angular/common';
import localeDa from '@angular/common/locales/da';

import {AppComponent} from './app/app.component';
import {routes} from './app/app.routes';
import {environment} from './environments/environment';

registerLocaleData(localeDa, 'da');

if (environment.production) {
  enableProdMode();
}

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes, withRouterConfig({onSameUrlNavigation: 'reload'})),
    provideHttpClient(withInterceptors([loadingInterceptor])),
    provideAnimations(),
    importProvidersFrom(ToastrModule.forRoot()),
  ],
}).catch(err => console.error(err));
