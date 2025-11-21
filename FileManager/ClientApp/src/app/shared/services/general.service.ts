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

  deleteFile(path: string) {
    return this.http.post('api/file/delete',{path:path});
  }

  deleteFiles(filesToDelete: string[]) {
    return this.http.post('api/file/deleteMultiple',filesToDelete);
  }
}
