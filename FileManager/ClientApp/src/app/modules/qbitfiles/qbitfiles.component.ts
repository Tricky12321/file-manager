import {Component, OnInit,} from '@angular/core';
import {CommonModule} from '@angular/common';
import {TorrentInfo} from "../../models/torrentInfo";
import {GeneralService} from "../../shared/services/general.service";


@Component({
  selector: 'qbitfiles',
  templateUrl: 'qbitfiles.component.html',
  styleUrls: ['qbitfiles.component.scss'],
  standalone: true,
  imports: [CommonModule]
})
export class QbitfilesComponent implements OnInit {
  public torrentList: string[] | null = null;
  public refreshing: boolean = false;

  constructor(public generalService: GeneralService) {

  }

  ngOnInit() {
    this.load(false);
  }

  load(clearCache: boolean = false) {
    this.refreshing = true;
    this.generalService.getTorrentFiles(clearCache).subscribe({
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
    this.load(true);
  }
}
