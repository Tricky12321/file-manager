import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {TorrentInfo} from "../../models/torrentInfo";
import {FileInfo} from "../../models/fileInfo";

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

}
