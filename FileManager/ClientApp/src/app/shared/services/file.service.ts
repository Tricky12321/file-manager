import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {TorrentInfo} from "../../models/torrentInfo";
import {FileInfo} from "../../models/fileInfo";
import {TableRequest} from "../../modules/simpleTable/simple-table.component";

@Injectable({
  providedIn: 'root'
})
export class FileService {
  constructor(private http: HttpClient) {
  }

  getFiles(path: string, hardlink: boolean | null = null, inQbit: boolean | null = null, folderInQbit: boolean | null = null, clearCache: boolean = false) {
    hardlink = hardlink?.toString() !== "null" ? hardlink : null;
    inQbit = inQbit?.toString() !== "null" ? inQbit : null;
    folderInQbit = folderInQbit?.toString() !== "null" ? folderInQbit : null;
    var queryParams = "?path=" + path;
    if (hardlink !== null && hardlink !== undefined) {
      queryParams += "&hardlink=" + hardlink;
    }
    if (inQbit !== null) {
      queryParams += "&inQbit=" + inQbit;
    }
    if (folderInQbit !== null) {
      queryParams += "&folderInQbit=" + folderInQbit;
    }
    if (clearCache !== null) {
      queryParams += "&clearCache=" + clearCache;
    }
    return this.http.get<FileInfo[]>('api/file/getFiles' + queryParams);
  }

  getFilesPost(path: string, tableRequest: TableRequest, hardlink: boolean | null = null, inQbit: boolean | null = null, folderInQbit: boolean | null = null, clearCache: boolean = false) {
    hardlink = hardlink?.toString() !== "null" ? hardlink : null;
    inQbit = inQbit?.toString() !== "null" ? inQbit : null;
    folderInQbit = folderInQbit?.toString() !== "null" ? folderInQbit : null;
    var queryParams = "?path=" + path;
    if (hardlink !== null && hardlink !== undefined) {
      queryParams += "&hardlink=" + hardlink;
    }
    if (inQbit !== null) {
      queryParams += "&inQbit=" + inQbit;
    }
    if (folderInQbit !== null) {
      queryParams += "&folderInQbit=" + folderInQbit;
    }
    if (clearCache !== null) {
      queryParams += "&clearCache=" + clearCache;
    }
    return this.http.post<FileInfo[]>('api/file/getFilesPost' + queryParams, tableRequest);
  }


  deleteFolder(path: string) {
    return this.http.post('api/file/deleteFolders',[path]);
  }

  deleteFolders(paths: string[]) {
    return this.http.post('api/file/deleteFolders', paths);
  }
}
