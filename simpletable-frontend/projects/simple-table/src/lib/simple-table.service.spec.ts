import { TestBed } from '@angular/core/testing';

import { SimpleTableService } from './simple-table.service';

describe('SimpleTableService', () => {
  let service: SimpleTableService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SimpleTableService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
