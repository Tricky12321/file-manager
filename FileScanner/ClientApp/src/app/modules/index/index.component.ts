import {Component, OnInit, ViewChild} from '@angular/core';
import {SomeService} from "../../shared/services/some.service";
import {Data} from "../../models/data";
import {GlobalFunctionsService} from "../../shared/services/globalFunctions.service";
import {Router} from "@angular/router";

@Component({
    selector: 'index',
    templateUrl: 'index.component.html',
    styleUrls: ['index.component.scss'],
    standalone: false
})

export class IndexComponent implements OnInit {
  public  data: Data | null = null;

  constructor(public someService: SomeService, public globalFunctionsService: GlobalFunctionsService, public router: Router) {
  }

  ngOnInit(): void {
    this.someService.getData().subscribe({
      next: (data) => {
        this.data = data;
      },
      error: (error) => {

      }
    })
  }
}
