import {Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class FileService {
  constructor(private http: HttpClient) {
  }

  deleteFolder(path: string) {
    return this.http.post('api/file/deleteFolders', [path]);
  }

  deleteFolders(paths: string[]) {
    return this.http.post('api/file/deleteFolders', paths);
  }
}
