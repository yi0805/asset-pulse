import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { AssetDetailPage } from './asset-detail-page';
import { AssetApiService } from '../asset-api.service';
import { Asset, AssetEvent, PagedResponse } from '../../models/api.models';

describe('AssetDetailPage', () => {
  const params = new BehaviorSubject(convertToParamMap({ id: '1' }));
  const asset: Asset = {
    id: 1,
    name: 'North pump',
    assetCode: 'PUMP-01',
    type: 'Pump',
    location: 'North',
    temperature: null,
    pressure: null,
    lastUpdated: '2026-09-18T00:00:00Z',
    status: 'Healthy',
  };
  const events: PagedResponse<AssetEvent> = {
    items: [
      {
        id: 1,
        assetId: 1,
        previousStatus: null,
        newStatus: 'Healthy',
        timestamp: '2026-09-18T00:00:00Z',
        description: 'Asset created',
      },
    ],
    page: 1,
    pageSize: 20,
    totalCount: 1,
  };
  let fixture: ComponentFixture<AssetDetailPage>;
  let api: { getAsset: ReturnType<typeof vi.fn>; getEvents: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    api = {
      getAsset: vi.fn().mockReturnValue(of(asset)),
      getEvents: vi.fn().mockReturnValue(of(events)),
    };
    await TestBed.configureTestingModule({
      imports: [AssetDetailPage],
      providers: [
        { provide: AssetApiService, useValue: api },
        { provide: ActivatedRoute, useValue: { paramMap: params } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AssetDetailPage);
    fixture.detectChanges();
  });

  it('shows asset fields, unavailable readings, and an initial status transition', () => {
    const page = fixture.nativeElement as HTMLElement;
    expect(page.textContent).toContain('North pump');
    expect(page.textContent).toContain('Unavailable');
    expect(page.textContent).toContain('Initial → Healthy');
    expect(page.querySelector<HTMLAnchorElement>('a[href="/assets/1/edit"]')).not.toBeNull();
  });

  it('requests a new history page and reports missing/error resources', () => {
    fixture.componentInstance.changeHistoryPage(2);
    expect(api.getEvents).toHaveBeenLastCalledWith(1, { page: 2 });

    api.getAsset.mockReturnValueOnce(
      throwError(() => ({ status: 404, error: { title: 'Not found' } })),
    );
    const errorFixture = TestBed.createComponent(AssetDetailPage);
    errorFixture.detectChanges();
    expect((errorFixture.nativeElement as HTMLElement).textContent).toContain('does not exist');
  });
});
