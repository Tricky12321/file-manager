import {BrowserModule} from "@angular/platform-browser";
import {NgModule} from "@angular/core";
import {FormsModule, ReactiveFormsModule} from "@angular/forms";
import {HTTP_INTERCEPTORS, HttpClientModule} from "@angular/common/http";
import {AppComponent} from "./app.component";
import {BrowserAnimationsModule} from "@angular/platform-browser/animations";
import {AppRoutingModule} from "./app-routing.module";
import {CommonModule, DatePipe, registerLocaleData} from "@angular/common";
import {IndexComponent} from "./modules/index/index.component";
import {NgbModule} from "@ng-bootstrap/ng-bootstrap";
import {GlobalFunctionsService} from "./shared/services/globalFunctions.service";
import {ToastrModule} from "ngx-toastr";
import localDa from "@angular/common/locales/da";
import {environment} from "../environments/environment";
import {ServiceWorkerModule} from "@angular/service-worker";
import {DataTablesModule} from "angular-datatables";
import {QbitdashboardComponent} from "./modules/qbitdashboard/qbitdashboard.component";
import {FileBrowserComponent} from "./modules/fileBrowser/fileBrowser.component";
import {QbitfilesComponent} from "./modules/qbitfiles/qbitfiles.component";

registerLocaleData(localDa, "da");

@NgModule({
  bootstrap: [AppComponent],
  declarations: [
    AppComponent,
    IndexComponent,
    QbitdashboardComponent,
    FileBrowserComponent,
    QbitfilesComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule,
    AppRoutingModule,
    ReactiveFormsModule,
    BrowserAnimationsModule,
    CommonModule,
    NgbModule,
    ToastrModule.forRoot(),
    ServiceWorkerModule.register("ngsw-worker.js", {
      enabled: environment.production,
      // Register the ServiceWorker as soon as the app is stable
      registrationStrategy: "registerWhenStable:5000",
    }),
    DataTablesModule,
  ],
  providers: [
    GlobalFunctionsService,
    DatePipe,
  ],
})
export class AppModule {
}
