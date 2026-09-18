import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { AssetsPage } from './assets-page';
import { AssetApiService } from '../asset-api.service';
import { PagedResponse, Asset } from '../../models/api.models';

describe('AssetsPage', () => {
  const queryParams = new BehaviorSubject(convertToParamMap({}));
  const assets: PagedResponse<Asset> = {
    items: [
      {
        id: 1,
        name: 'North pump',
        assetCode: 'PUMP-01',
        type: 'Pump',
        location: 'North',
        temperature: null,
        pressure: null,
        lastUpdated: '2026-09-18T00:00:00Z',
        status: 'Healthy',
      },
    ],
    page: 1,
    pageSize: 20,
    totalCount: 21,
  };
  let fixture: ComponentFixture<AssetsPage>;
  let api: { getAssets: ReturnType<typeof vi.fn> };
  const router = { navigate: vi.fn().mockResolvedValue(true) };

  beforeEach(async () => {
    queryParams.next(convertToParamMap({}));
    api = { getAssets: vi.fn().mockReturnValue(of(assets)) };
    await TestBed.configureTestingModule({
      imports: [AssetsPage],
      providers: [
        { provide: AssetApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { queryParamMap: queryParams } },
        { provide: Router, useValue: router },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AssetsPage);
    fixture.detectChanges();
  });

  it('loads and displays assets, unavailable readings, and detail/create links', () => {
    const page = fixture.nativeElement as HTMLElement;
    expect(api.getAssets).toHaveBeenCalledWith({
      search: undefined,
      type: undefined,
      location: undefined,
      status: undefined,
      page: undefined,
      pageSize: undefined,
    });
    expect(page.textContent).toContain('North pump');
    expect(page.textContent).toContain('Unavailable');
    expect(page.textContent).toContain('Create asset');
    expect(page.textContent).toContain('Details');
  });

  it('submits filters into query parameters and supports pagination', () => {
    fixture.componentInstance.filters.patchValue({ search: ' pump ', status: 'Healthy' });
    fixture.componentInstance.apply();
    fixture.componentInstance.changePage(2);

    expect(router.navigate).toHaveBeenNthCalledWith(
      1,
      [],
      expect.objectContaining({
        queryParams: expect.objectContaining({ search: 'pump', status: 'Healthy' }),
      }),
    );
    expect(router.navigate).toHaveBeenNthCalledWith(
      2,
      [],
      expect.objectContaining({ queryParams: expect.objectContaining({ page: 2 }) }),
    );
  });

  it('shows an API error and retries instead of showing an empty result', () => {
    api.getAssets
      .mockReturnValueOnce(throwError(() => ({ status: 503, error: { title: 'Unavailable' } })))
      .mockReturnValue(of(assets));
    const retryFixture = TestBed.createComponent(AssetsPage);
    retryFixture.detectChanges();
    expect((retryFixture.nativeElement as HTMLElement).textContent).toContain('Unavailable');
    const callsBeforeRetry = api.getAssets.mock.calls.length;
    retryFixture.componentInstance.retry();
    expect(api.getAssets).toHaveBeenCalledTimes(callsBeforeRetry + 1);
  });
});
