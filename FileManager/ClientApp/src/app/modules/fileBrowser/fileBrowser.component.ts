import {Component, OnInit, ViewChild,} from '@angular/core';
import {TorrentInfo} from "../../models/torrentInfo";
import {GeneralService} from "../../shared/services/general.service";
import {FileService} from "../../shared/services/file.service";
import {FileInfo} from 'src/app/models/fileInfo';
import {DialogService} from "../../shared/services/dialogs/dialog.service";
import {ToastrService} from "ngx-toastr";
import {SimpleTableComponent, TableRequest} from "../simpleTable/simple-table.component";
import {Observable} from "rxjs";
import {HttpClient} from "@angular/common/http";


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
  public needConfirm: boolean = true;

  // Variable to link to simple-table component, to call function in the component
  @ViewChild(SimpleTableComponent) simpleTableComponent: SimpleTableComponent | undefined;
  public clearCache: boolean | null = null;
  public hashDuplicate: boolean | null = null;

  constructor(public http: HttpClient, public generalService: GeneralService, public fileService: FileService, public dialogSrv: DialogService, public toastrService: ToastrService) {

    this.hardlinkFilter = localStorage.getItem("hardlink") == "null" ? null : (localStorage.getItem("hardlink") == "true");
    this.inQbitFilter = localStorage.getItem("inQbit") == "null" ? null : (localStorage.getItem("inQbit") == "true");
    this.folderInQbitFilter = localStorage.getItem("folderInQbit") == "null" ? null : (localStorage.getItem("folderInQbit") == "true");
    this.needConfirm = localStorage.getItem("needConfirm") == "null" ? true : (localStorage.getItem("folderInQbit") == "true");
    this.hashDuplicate = localStorage.getItem("hashDuplicate") == "null" ? true : (localStorage.getItem("hashDuplicate") == "true");
  }

  buildUrl() {
    let hardlink = this.hardlinkFilter?.toString() !== "null" ? this.hardlinkFilter : null;
    let inQbit = this.inQbitFilter?.toString() !== "null" ? this.inQbitFilter : null;
    let folderInQbit = this.folderInQbitFilter?.toString() !== "null" ? this.folderInQbitFilter : null;
    let hashDuplicate = this.hashDuplicate?.toString() !== "null" ? this.hashDuplicate : null;
    var queryParams = "?path=" + this.filePath;
    if (hardlink !== null && hardlink !== undefined) {
      queryParams += "&hardlink=" + hardlink;
    }
    if (inQbit !== null) {
      queryParams += "&inQbit=" + inQbit;
    }
    if (folderInQbit !== null) {
      queryParams += "&folderInQbit=" + folderInQbit;
    }
    if (hashDuplicate !== null) {
      queryParams += "&hashDuplicate=" + hashDuplicate;
    }
    if (this.clearCache !== null) {
      queryParams += "&clearCache=" + this.clearCache;
      if (this.clearCache) {
        this.clearCache = false;
      }
    }
    return 'api/file/getFilesPost' + queryParams;
  }

  ngOnInit() {
    if (window.location.pathname == "/tv") {
      this.filePath = "/torrent/TV"
    } else if (window.location.pathname == "/film") {
      this.filePath = "/torrent/Film"
    }
    this.load();
  }

  deleteFile(fileInfo: FileInfo) {
    if (!this.needConfirm) {
      this.dialogSrv.openConfirmDialog("File to delete: " + fileInfo.path, "Are you sure you want to delete this file?").afterClosed().subscribe((result) => {
        if (result == true) {
          this.generalService.deleteFile(fileInfo.path).subscribe({
            next: (data) => {
              this.load()
            },
            error: () => {
              this.toastrService.error("Error deleting file");
            }
          });
        }
      });
    } else {
      this.generalService.deleteFile(fileInfo.path).subscribe({
        next: (data) => {
          this.load();
        },
        error: () => {
          this.toastrService.error("Error deleting file");
        }
      });
    }
  }

  getFiles() {
    var data = this.simpleTableComponent?.data;
    if (data != null) {
      return data as FileInfo[];
    } else {
      return [];
    }
  }
  deleteSelectedFiles() {
    const filesToDelete = this.getFiles()!.map(x => x.path);
    if (filesToDelete.length == 0) {
      this.toastrService.info("No files selected");
      return;
    }
    this.dialogSrv.openConfirmDialog("Are you sure you want to delete all selected files?").afterClosed().subscribe((result) => {
      if (result == true) {
        this.generalService.deleteFiles(filesToDelete).subscribe({
          next: (data) => {
            this.load();
          },
          error: () => {
            this.toastrService.error("Error deleting files, please refresh and try again");
          }
        });
      } else {
        this.toastrService.warning("Not deleting files");
      }
    });
  }

  paramsChanged() {
    // Store all the filter values in local storage
    localStorage.setItem("hardlink", this.hardlinkFilter?.toString() ?? "null");
    localStorage.setItem("inQbit", this.inQbitFilter?.toString() ?? "null");
    localStorage.setItem("folderInQbit", this.folderInQbitFilter?.toString() ?? "null");
    localStorage.setItem("needConfirm", this.needConfirm.toString());
    localStorage.setItem("hashDuplicate", this.hashDuplicate?.toString() ?? "null");
    this.load()
  }

  load(clearCache: boolean = false) {
    this.clearCache = clearCache;
    if (this.simpleTableComponent != null) {
      this.simpleTableComponent.update();
    }
  }

  toggleSelectedFiles() {
    var files = this.getFiles();
    var anySelected = files.find(x => x.selected == true);
    if (anySelected != null) {
      // Deselect all
      files.forEach(x => x.selected = false);
    } else {
      // Select all
      files.forEach(x => x.selected = true);
    }
  }
}
