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
  public refreshing: boolean = false;

  constructor(public generalService: GeneralService) {

  }

  ngOnInit() {
    this.load();
  }

  load() {
    this.refreshing = true;
    this.generalService.getData().subscribe({
      next: (data) => {
        this.torrentList = data;
        this.refreshing = false;
      },
      error: () => {
        this.refreshing = false;
      },
    });
  }

  refreshData() {
    this.torrentList = null;
    this.load();
  }


}
