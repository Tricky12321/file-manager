import {Component, OnInit,} from '@angular/core';
import {CommonModule} from '@angular/common';
import {TorrentInfo} from "../../models/torrentInfo";
import {GeneralService} from "../../shared/services/general.service";


@Component({
  selector: 'qbitdashboard',
  templateUrl: 'qbitdashboard.component.html',
  styleUrls: ['qbitdashboard.component.scss'],
  standalone: true,
  imports: [CommonModule]
})
export class QbitdashboardComponent implements OnInit {
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
    });
  }

  refreshData() {
    this.torrentList = null;
    this.generalService.refreshData().subscribe({
      next: (data) => {
        this.torrentList = data;
      },
    });
  }


}
