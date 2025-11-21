import {Component, OnInit,} from '@angular/core';
import {TorrentInfo} from "../../models/torrentInfo";
import {GeneralService} from "../../shared/services/general.service";


@Component({
  selector: 'qbitfiles',
  templateUrl: 'qbitfiles.component.html',
  styleUrls: ['qbitfiles.component.scss'],
  standalone: false
})
export class QbitfilesComponent implements OnInit {
  public torrentList: string[] | null = null;
  public dtOptions: DataTables.Settings = {
    paging: false,
  }

  constructor(public generalService: GeneralService) {

  }

  ngOnInit() {
    this.load();
  }

  load() {
    this.generalService.getTorrentFiles().subscribe({
      next: (data) => {
        this.torrentList = data;
      },
    });
  }

  refreshData() {
    this.torrentList = null;
    this.generalService.getTorrentFiles(true).subscribe({
      next: (data) => {
        this.torrentList = data;
      },
    });
  }


}
