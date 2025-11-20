import {Component, OnInit,} from '@angular/core';
import {TorrentInfo} from "../../models/torrentInfo";
import {GeneralService} from "../../shared/services/general.service";
import {FileService} from "../../shared/services/file.service";
import {FileInfo} from 'src/app/models/fileInfo';
import {DialogService} from "../../shared/services/dialogs/dialog.service";
import {ToastrService} from "ngx-toastr";


@Component({
  selector: 'filebrowser',
  templateUrl: 'fileBrowser.component.html',
  styleUrls: ['fileBrowser.component.scss'],
  standalone: false
})
export class FileBrowserComponent implements OnInit {
  public dtOptions: DataTables.Settings = {
    paging: false,
  }
  public loading: boolean = false;
  public filePath!: string;
  public fileList: FileInfo[] | null = null;
  public hardlinkFilter: boolean | null = null;
  public inQbitFilter: boolean | null = null;
  public folderInQbitFilter: boolean | null = null;

  constructor(public generalService: GeneralService, public fileService: FileService, public dialogSrv: DialogService, public toastrService: ToastrService) {

    this.hardlinkFilter = localStorage.getItem("hardlink") == "null" ? null : (localStorage.getItem("hardlink") == "true");
    this.inQbitFilter = localStorage.getItem("inQbit") == "null" ? null : (localStorage.getItem("inQbit") == "true");
    this.folderInQbitFilter = localStorage.getItem("folderInQbit") == "null" ? null : (localStorage.getItem("folderInQbit") == "true");
  }

  ngOnInit() {
    if (window.location.pathname == "/tv") {
      this.filePath = "/torrent/TV"
    } else if (window.location.pathname == "/film") {
      this.filePath = "/torrent/Film"
    }
    this.load();
  }

  load(clearCache: boolean = false) {
    if (!this.loading) {
      this.fileList = null;
      this.loading = true;
      this.fileService.getFiles(this.filePath, this.hardlinkFilter, this.inQbitFilter, this.folderInQbitFilter, clearCache).subscribe({
        next: (data) => {
          this.fileList = data;
          this.loading = false;
        },
        error: () => {
          this.toastrService.error("Error loading files");
          this.loading = false;
        }
      });
    }

  }


  deleteFile(fileInfo: FileInfo) {
    this.dialogSrv.openConfirmDialog("File to delete: " + fileInfo.path, "Are you sure you want to delete this file?").afterClosed().subscribe((result) => {
      if (result == true) {
        this.generalService.deleteFile(fileInfo.path).subscribe({
          next: (data) => {
            this.fileList = this.fileList!.filter(item => item.path !== fileInfo.path);
          },
          error: () => {
            this.toastrService.error("Error deleting file");
          }
        });
      }
    });
  }

  paramsChanged() {
    // Store all the filter values in local storage
    localStorage.setItem("hardlink", this.hardlinkFilter?.toString() ?? "null");
    localStorage.setItem("inQbit", this.inQbitFilter?.toString() ?? "null");
    localStorage.setItem("folderInQbit", this.folderInQbitFilter?.toString() ?? "null");
    this.load()
  }
}
