import {Injectable} from "@angular/core";
@Injectable()
export class GlobalFunctionsService {
  getUrl() {
    return window.location.pathname;
  }

}


