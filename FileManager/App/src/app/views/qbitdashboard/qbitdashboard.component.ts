import {NgStyle} from '@angular/common';
import {
  Component,
  DestroyRef,
  DOCUMENT,
  effect,
  inject,
  OnInit,
  Renderer2,
  signal,
  WritableSignal
} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule} from '@angular/forms';
import {
  AvatarComponent,
  ButtonDirective,
  ButtonGroupComponent,
  CardBodyComponent,
  CardComponent,
  CardFooterComponent,
  CardHeaderComponent,
  ColComponent,
  FormCheckLabelDirective,
  GutterDirective,
  ProgressComponent,
  RowComponent,
  TableDirective
} from '@coreui/angular';
import {ChartjsComponent} from '@coreui/angular-chartjs';
import {IconDirective} from '@coreui/icons-angular';
import {WidgetsBrandComponent} from '../widgets/widgets-brand/widgets-brand.component';
import {WidgetsDropdownComponent} from '../widgets/widgets-dropdown/widgets-dropdown.component';
import {GeneralService} from '../../shared/general.service';
import {TorrentInfo} from '../../models/torrentInfo';

@Component({
  templateUrl: 'qbitdashboard.component.html',
  styleUrls: ['qbitdashboard.component.scss'],
  imports: [WidgetsDropdownComponent, CardComponent, CardBodyComponent, RowComponent, ColComponent, ButtonDirective, IconDirective, ReactiveFormsModule, ButtonGroupComponent, FormCheckLabelDirective, ChartjsComponent, NgStyle, CardFooterComponent, GutterDirective, ProgressComponent, WidgetsBrandComponent, CardHeaderComponent, TableDirective, AvatarComponent]
})
export class DashboardComponent implements OnInit {
  public torrentList: TorrentInfo[] | null = null;
  constructor(public generalService: GeneralService) {

  }

  ngOnInit() {
    this.load();
  }

  load() {
    this.generalService.getData().subscribe({
      next: (data) => {
        this.torrentList = data;
      },
      error: (err) => {
      }
    });
  }
}
