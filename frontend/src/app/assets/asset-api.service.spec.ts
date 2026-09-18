import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Asset, AssetEvent, PagedResponse } from '../models/api.models';
import { AssetApiService } from './asset-api.service';

describe('AssetApiService', () => {
  let service: AssetApiService;
  let httpTesting: HttpTestingController;

  const asset: Asset = {
    id: 1,
    name: 'Boiler feed pump',
    assetCode: 'PUMP-001',
    type: 'Pump',
    location: 'Boiler house',
    temperature: 55.4,
    pressure: null,
    lastUpdated: '2026-09-18T01:00:00Z',
    status: 'Warning',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AssetApiService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('gets assets with supplied query parameters', () => {
    let response: PagedResponse<Asset> | undefined;

    service
      .getAssets({
        search: 'pump',
        type: 'Pump',
        location: 'Boiler house',
        status: 'Warning',
        page: 0,
        pageSize: 0,
      })
      .subscribe((value) => (response = value));

    const request = httpTesting.expectOne(
      '/api/assets?search=pump&type=Pump&location=Boiler%20house&status=Warning&page=0&pageSize=0',
    );
    expect(request.request.method).toBe('GET');
    request.flush({
      items: [asset],
      page: 0,
      pageSize: 0,
      totalCount: 1,
    } satisfies PagedResponse<Asset>);

    expect(response?.items[0].assetCode).toBe('PUMP-001');
  });

  it('does not send omitted asset query parameters', () => {
    service.getAssets().subscribe();

    const request = httpTesting.expectOne('/api/assets');
    expect(request.request.params.keys()).toEqual([]);
    request.flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    } satisfies PagedResponse<Asset>);
  });

  it('gets an asset by id', () => {
    let response: Asset | undefined;
    service.getAsset(12).subscribe((value) => (response = value));

    const request = httpTesting.expectOne('/api/assets/12');
    request.flush(asset);

    expect(response?.id).toBe(1);
  });

  it('gets paged asset events', () => {
    const event: AssetEvent = {
      id: 3,
      assetId: 12,
      previousStatus: 'Healthy',
      newStatus: 'Warning',
      timestamp: '2026-09-18T01:00:00Z',
      description: 'Warning alarm raised.',
    };
    let response: PagedResponse<AssetEvent> | undefined;
    service.getEvents(12, { page: 2, pageSize: 10 }).subscribe((value) => (response = value));

    const request = httpTesting.expectOne('/api/assets/12/events?page=2&pageSize=10');
    request.flush({
      items: [event],
      page: 2,
      pageSize: 10,
      totalCount: 1,
    } satisfies PagedResponse<AssetEvent>);

    expect(response?.items[0].newStatus).toBe('Warning');
  });
});
