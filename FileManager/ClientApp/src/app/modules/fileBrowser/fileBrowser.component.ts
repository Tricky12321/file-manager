import {Component, OnInit, ViewChild,} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {GeneralService} from "../../shared/services/general.service";
import {FileService} from "../../shared/services/file.service";
import {FileInfo} from 'src/app/models/fileInfo';
import {DialogService} from "../../shared/services/dialogs/dialog.service";
import {ToastrService} from "ngx-toastr";
import {SimpleTableComponent} from "../simpleTable/simple-table.component";
import {HttpClient} from "@angular/common/http";
import {ActivatedRoute, Router} from "@angular/router";
import {UrlStateService} from "../../shared/services/url-state.service";


@Component({
  selector: 'filebrowser',
  templateUrl: 'fileBrowser.component.html',
  styleUrls: ['fileBrowser.component.scss'],
  standalone: true,
  imports: [CommonModule, FormsModule, SimpleTableComponent]
})
export class FileBrowserComponent implements OnInit {
  public loading: boolean = false;
  public filePath: string = '';
  public fileList: FileInfo[] | null = null;
  public hardlinkFilter: boolean | null = null;
  public inQbitFilter: boolean | null = null;
  public folderInQbitFilter: boolean | null = null;
  public needConfirm: boolean = true;

  // Variable to link to simple-table component, to call function in the component
  @ViewChild(SimpleTableComponent) simpleTableComponent: SimpleTableComponent | undefined;
  public clearCache: boolean | null = null;
  public hashDuplicate: boolean | null = null;
  public currentUrl!: string;
  public folderPath: string = '';
  public folderMode: boolean = false;
  public smallMode: boolean = false;
  public emptyMode: boolean = false;
  public sampleMode: boolean = false;
  public samplePath: string = '';
  // True while a delete request is in flight; disables action buttons and shows a spinner.
  public busy: boolean = false;


  constructor(public http: HttpClient, public generalService: GeneralService, public fileService: FileService, public dialogSrv: DialogService, public toastrService: ToastrService, public router: Router, public route: ActivatedRoute, public urlState: UrlStateService) {

    // Filters restore from the URL first (so a refresh keeps them), then fall back to localStorage.
    this.hardlinkFilter = this.readTriStateFilter("hardlink");
    this.inQbitFilter = this.readTriStateFilter("inQbit");
    this.folderInQbitFilter = this.readTriStateFilter("folderInQbit");
    this.hashDuplicate = this.readTriStateFilter("hashDuplicate");
    const needConfirmStored = localStorage.getItem("needConfirm");
    this.needConfirm = needConfirmStored == null || needConfirmStored == "null" ? true : (needConfirmStored == "true");
  }

  // Tri-state filter (true/false/null). URL query param wins, then localStorage, else null.
  private readTriStateFilter(key: string): boolean | null {
    const raw = this.urlState.get(key) ?? localStorage.getItem(key);
    if (raw == null || raw === "null") {
      return null;
    }
    return raw === "true";
  }

  buildUrl(): string {
    if (this.sampleMode) {
      this.currentUrl = 'api/file/getSampleFiles?path=' + this.samplePath;
      return this.currentUrl;
    }
    if (this.filePath != "") {
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
      this.currentUrl = 'api/file/getFilesPost' + queryParams;
      return this.currentUrl;
    } else if (this.folderPath != "" && !this.smallMode && !this.emptyMode) {
      let folderInQbit = this.folderInQbitFilter?.toString() !== "null" ? this.folderInQbitFilter : null;
      var queryParams = "?path=" + this.folderPath;
      if (folderInQbit !== null) {
        queryParams += "&folderInQbit=" + folderInQbit;
      }
      if (this.clearCache !== null) {
        queryParams += "&clearCache=" + this.clearCache;
        if (this.clearCache) {
          this.clearCache = false;
        }
      }
      this.currentUrl = 'api/file/getFoldersPost' + queryParams;
      return this.currentUrl;
    } else if (this.folderPath != "" && this.smallMode) {
      this.currentUrl = 'api/file/getSmallFolders?path='+this.folderPath;
      return this.currentUrl;
    } else if (this.folderPath != "" && this.emptyMode) {
      this.currentUrl = 'api/file/getEmptyFolders?path='+this.folderPath;
      return this.currentUrl;
    }
    throw new Error("No path set for file browser");
  }

  ngOnInit() {
    // Configuration comes from the route's data ({scanPath, mode}); /browse takes its
    // path from the ?path= query param instead.
    const data = this.route.snapshot.data as { scanPath?: string; mode?: string };
    const pathParam = this.route.snapshot.queryParamMap.get('path');

    this.filePath = '';
    this.folderPath = '';
    this.folderMode = false;
    this.emptyMode = false;
    this.smallMode = false;
    this.sampleMode = false;
    this.samplePath = '';

    const scanPath = data.scanPath ?? '';
    switch (data.mode) {
      case 'files':
        this.filePath = scanPath;
        break;
      case 'samples':
        this.samplePath = scanPath;
        this.sampleMode = true;
        break;
      case 'folders':
        this.folderPath = scanPath;
        this.folderMode = true;
        break;
      case 'empty':
        this.folderPath = scanPath;
        this.folderMode = true;
        this.emptyMode = true;
        break;
      case 'small':
        this.folderPath = scanPath;
        this.folderMode = true;
        this.smallMode = true;
        break;
      case 'browse':
      default:
        this.filePath = pathParam ?? '';
        break;
    }

    // A ?path= query param always wins for the file view (deep links / "Open folder").
    if (pathParam && data.mode !== 'folders' && data.mode !== 'empty' && data.mode !== 'small' && data.mode !== 'samples') {
      this.filePath = pathParam;
    }

    this.load();
  }

  deleteFile(fileInfo: FileInfo) {
    if (this.needConfirm) {
      this.dialogSrv.openConfirmDialog("File to delete: " + fileInfo.path, "Are you sure you want to delete this file?").afterClosed().subscribe((result) => {
        if (result == true) {
          this.busy = true;
          this.generalService.deleteFile(fileInfo.path, this.filePath).subscribe({
            next: (data) => {
              this.busy = false;
              this.load()
            },
            error: () => {
              this.busy = false;
              this.toastrService.error("Error deleting file");
            }
          });
        }
      });
    } else {
      this.busy = true;
      this.generalService.deleteFile(fileInfo.path).subscribe({
        next: (data) => {
          this.busy = false;
          this.load();
        },
        error: () => {
          this.busy = false;
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

  // Opens the deselectable confirm dialog with every currently-selected sample, so the
  // user can review and uncheck false positives before bulk-deleting the rest.
  deleteAllSamples() {
    const candidates = this.getFiles().filter(x => x.selected).map(x => x.path);
    if (candidates.length == 0) {
      this.toastrService.info("No sample files selected");
      return;
    }
    this.dialogSrv.openSelectionDialog(candidates, "Delete sample files")
      .afterClosed().subscribe((paths) => {
      if (paths == null) {
        this.toastrService.warning("Not deleting files");
        return;
      }
      if (paths.length == 0) {
        return;
      }
      this.busy = true;
      this.generalService.deleteFiles(paths).subscribe({
        next: () => {
          this.busy = false;
          this.toastrService.success(`Deleted ${paths.length} sample file(s)`);
          this.load();
        },
        error: () => {
          this.busy = false;
          this.toastrService.error("Error deleting files, please refresh and try again");
        }
      });
    });
  }

  deleteSelectedFiles() {
    const filesToDelete = this.getFiles()!.map(x => x.path);
    if (filesToDelete.length == 0) {
      this.toastrService.info("No files selected");
      return;
    }
    this.dialogSrv.openConfirmDialog("Are you sure you want to delete all selected files?").afterClosed().subscribe((result) => {
      if (result == true) {
        this.busy = true;
        this.generalService.deleteFiles(filesToDelete).subscribe({
          next: (data) => {
            this.busy = false;
            this.load();
          },
          error: () => {
            this.busy = false;
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
    // Mirror the filters into the URL so a refresh restores them.
    this.urlState.patch({
      hardlink: this.hardlinkFilter,
      inQbit: this.inQbitFilter,
      folderInQbit: this.folderInQbitFilter,
      hashDuplicate: this.hashDuplicate,
    });
    this.load()
  }

  load(clearCache: boolean = false) {
    this.clearCache = clearCache;
    if (this.simpleTableComponent != null) {
      this.simpleTableComponent.update(this.buildUrl());
      this.clearCache = false;
      this.simpleTableComponent.url = this.buildUrl();
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

  openFolder(fileInfo: FileInfo) {
    var folderPath: string;
    if (this.folderMode) {
      folderPath = fileInfo.path;
    } else {
      folderPath = fileInfo.folderPath;
    }
    this.router.navigate(['/browse'], {queryParams: {path: folderPath}})
  }

  deleteFolder(fileInfo: FileInfo) {
    this.busy = true;
    this.fileService.deleteFolder(fileInfo.path).subscribe({
      next: (data) => {
        this.busy = false;
        this.toastrService.success("Folder deleted");
        this.load();
      },
      error: () => {
        this.busy = false;
        this.toastrService.error("Error deleting folder");
      }
    });
  }

  deleteSelectedFolders() {
    this.busy = true;
    this.fileService.deleteFolders(this.getFiles().filter(x => x.selected)!.map(x => x.path)).subscribe({
      next: (data) => {
        this.busy = false;
        this.toastrService.success("Folders deleted");
        this.load();
      },
      error: () => {
        this.busy = false;
        this.toastrService.error("Error deleting folders");
      }
    });
  }
}
