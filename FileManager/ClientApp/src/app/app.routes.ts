import {Routes} from '@angular/router';
import {QbitdashboardComponent} from './modules/qbitdashboard/qbitdashboard.component';
import {FileBrowserComponent} from './modules/fileBrowser/fileBrowser.component';
import {QbitfilesComponent} from './modules/qbitfiles/qbitfiles.component';

// FileBrowser is driven entirely by route data instead of inspecting window.location.
// mode: 'files' | 'folders' | 'empty' | 'small' | 'browse'
export const routes: Routes = [
  {path: 'qb', component: QbitdashboardComponent},
  {path: 'qbfiles', component: QbitfilesComponent},
  {path: 'files/tv', component: FileBrowserComponent, data: {scanPath: '/torrent/TV', mode: 'files'}},
  {path: 'files/film', component: FileBrowserComponent, data: {scanPath: '/torrent/Film', mode: 'files'}},
  {path: 'directories/tv', component: FileBrowserComponent, data: {scanPath: '/torrent/TV', mode: 'folders'}},
  {path: 'directories/film', component: FileBrowserComponent, data: {scanPath: '/torrent/Film', mode: 'folders'}},
  {path: 'directories/empty/tv', component: FileBrowserComponent, data: {scanPath: '/torrent/TV', mode: 'empty'}},
  {path: 'directories/empty/film', component: FileBrowserComponent, data: {scanPath: '/torrent/Film', mode: 'empty'}},
  {path: 'directories/small/tv', component: FileBrowserComponent, data: {scanPath: '/torrent/TV', mode: 'small'}},
  {path: 'directories/small/film', component: FileBrowserComponent, data: {scanPath: '/torrent/Film', mode: 'small'}},
  {path: 'browse', component: FileBrowserComponent, data: {mode: 'browse'}},
  {path: '', pathMatch: 'full', redirectTo: 'qb'},
];
