import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {TorrentInfo} from "../../models/torrentInfo";

@Injectable({
  providedIn: 'root'
})
export class GeneralService {
  constructor(private http: HttpClient) {
  }

  getData() {
    return this.http.get<TorrentInfo[]>('api/qbit');
  }

  refreshData() {
    return this.http.get<TorrentInfo[]>('api/qbit/true');
  }

  deleteFile(path: string, filePath: string| null = null) {
    if (filePath === null) {
      return this.http.post('api/file/delete',{path:path});

    }
    return this.http.post('api/file/delete?filePath='+filePath,{path:path});
  }

  deleteFiles(filesToDelete: string[], filePath: string| null = null) {
    if (filePath === null) {
      return this.http.post('api/file/deleteMultiple',filesToDelete);

    }
    return this.http.post('api/file/deleteMultiple?filePath='+filePath,filesToDelete);
  }

  getTorrentFiles(clearCache: boolean = false) {
    if (clearCache) {
      return this.http.get<string[]>('api/qbit/torrentfiles/true');
    } else {
      return this.http.get<string[]>('api/qbit/torrentfiles');
    }
  }
}
