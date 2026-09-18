import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Alarm, PagedResponse } from '../models/api.models';
import { AlarmApiService } from './alarm-api.service';

describe('AlarmApiService', () => {
  let service: AlarmApiService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AlarmApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('gets alarms with representative combined filters', () => {
    const alarm: Alarm = {
      id: 5,
      assetId: 4,
      assetCode: 'MOTOR-004',
      assetName: 'Conveyor motor',
      severity: 'Critical',
      message: 'Temperature alarm',
      status: 'Active',
      createdAt: '2026-09-18T01:00:00Z',
      acknowledgedAt: null,
      resolvedAt: null,
    };
    let response: PagedResponse<Alarm> | undefined;

    service
      .getAlarms({ assetId: 0, severity: 'Critical', status: 'Active', page: 2, pageSize: 50 })
      .subscribe((value) => (response = value));

    const request = httpTesting.expectOne(
      '/api/alarms?assetId=0&severity=Critical&status=Active&page=2&pageSize=50',
    );
    expect(request.request.method).toBe('GET');
    request.flush({
      items: [alarm],
      page: 2,
      pageSize: 50,
      totalCount: 1,
    } satisfies PagedResponse<Alarm>);

    expect(response?.items[0].severity).toBe('Critical');
  });

  it('does not send omitted alarm filters', () => {
    service.getAlarms().subscribe();

    const request = httpTesting.expectOne('/api/alarms');
    expect(request.request.params.keys()).toEqual([]);
    request.flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    } satisfies PagedResponse<Alarm>);
  });
});
