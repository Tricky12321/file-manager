import {NgModule} from '@angular/core';
import {Routes, RouterModule} from '@angular/router';
import {IndexComponent} from "./modules/index/index.component";
import {QbitdashboardComponent} from "./modules/qbitdashboard/qbitdashboard.component";
import {FileBrowserComponent} from "./modules/fileBrowser/fileBrowser.component";

const routes: Routes = [
  {path: 'qb', component: QbitdashboardComponent},
  {path: 'tv', component: FileBrowserComponent},
  {path: 'film', component: FileBrowserComponent},
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {onSameUrlNavigation: 'reload'})],
  exports: [RouterModule]
})
export class AppRoutingModule {
}
