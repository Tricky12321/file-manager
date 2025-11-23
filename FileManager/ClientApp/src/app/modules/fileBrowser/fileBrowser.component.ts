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

  constructor(public http: HttpClient, public generalService: GeneralService, public fileService: FileService, public dialogSrv: DialogService, public toastrService: ToastrService) {

    this.hardlinkFilter = localStorage.getItem("hardlink") == "null" ? null : (localStorage.getItem("hardlink") == "true");
    this.inQbitFilter = localStorage.getItem("inQbit") == "null" ? null : (localStorage.getItem("inQbit") == "true");
    this.folderInQbitFilter = localStorage.getItem("folderInQbit") == "null" ? null : (localStorage.getItem("folderInQbit") == "true");
    this.needConfirm = localStorage.getItem("needConfirm") == "null" ? true : (localStorage.getItem("folderInQbit") == "true");
  }

  getTableData(tableRequest: TableRequest) {
    let hardlink = this.hardlinkFilter?.toString() !== "null" ? this.hardlinkFilter : null;
    let inQbit = this.inQbitFilter?.toString() !== "null" ? this.inQbitFilter : null;
    let folderInQbit = this.folderInQbitFilter?.toString() !== "null" ? this.folderInQbitFilter : null;
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
    /*
    if (clearCache !== null) {
      queryParams += "&clearCache=" + clearCache;
    }
    */
    return this.http.post<FileInfo[]>('api/file/getFilesPost' + queryParams, tableRequest);
    //return this.fileService.getFilesPost(this.filePath, tableRequest, this.hardlinkFilter, this.inQbitFilter, this.folderInQbitFilter);
  }

  buildUrl() {
    let hardlink = this.hardlinkFilter?.toString() !== "null" ? this.hardlinkFilter : null;
    let inQbit = this.inQbitFilter?.toString() !== "null" ? this.inQbitFilter : null;
    let folderInQbit = this.folderInQbitFilter?.toString() !== "null" ? this.folderInQbitFilter : null;
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
    return 'api/file/getFilesPost' + queryParams;
  }

  ngOnInit() {
    if (window.location.pathname == "/tv") {
      this.filePath = "/torrent/TV"
    } else if (window.location.pathname == "/film") {
      this.filePath = "/torrent/Film"
    }
    //this.load();
  }
/*
  load(clearCache: boolean = false) {
    if (!this.loading) {
      this.fileList = null;
      this.loading = true;
      this.fileService.getFiles(this.filePath, this.hardlinkFilter, this.inQbitFilter, this.folderInQbitFilter, clearCache).subscribe({
        next: (data) => {
          this.fileList = data;
          this.fileList.map((file: FileInfo) => {
            file.selected = false;
          })
          this.loading = false;
        },
        error: () => {
          this.toastrService.error("Error loading files");
          this.loading = false;
        }
      });
    }
  }
*/


  deleteFile(fileInfo: FileInfo) {
    if (this.needConfirm == false) {
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
    } else {
      this.generalService.deleteFile(fileInfo.path).subscribe({
        next: (data) => {
          this.fileList = this.fileList!.filter(item => item.path !== fileInfo.path);
        },
        error: () => {
          this.toastrService.error("Error deleting file");
        }
      });
    }
  }

  deleteSelectedFiles() {
    const filesToDelete = this.fileList!.filter(file => file.selected).map(x => x.path);
    if (filesToDelete.length == 0) {
      this.toastrService.info("No files selected");
      return;
    }
    this.dialogSrv.openConfirmDialog("Are you sure you want to delete all selected files?").afterClosed().subscribe((result) => {
      if (result == true) {
          this.generalService.deleteFiles(filesToDelete).subscribe({
          next: (data) => {
            this.fileList = this.fileList!.filter(item => !filesToDelete.includes(item.path));
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
    if (this.simpleTableComponent != null) {
      this.simpleTableComponent.update();
    }
    //this.load()
  }
/*
  toggleSelectedFiles() {
    // if any selected, select none, else select all
    const anySelected = this.fileList!.some(file => file.selected);
    if (anySelected) {
      this.fileList!.forEach(file => file.selected = false);
    } else {
      this.fileList!.forEach(file => file.selected = true);
    }

  }
  */
}
