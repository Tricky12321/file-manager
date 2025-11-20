import {Injectable} from "@angular/core";
import {HttpClient} from "@angular/common/http";
import {Data} from "../../models/data";

@Injectable({
  providedIn: 'root'
})
export class SomeService {
  constructor(private http: HttpClient) {
  }

  getData() {
    return this.http.get<Data>('api/index');
  }

}
