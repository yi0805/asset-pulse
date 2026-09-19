import { provideLocationMocks } from '@angular/common/testing';
import { provideRouter, Router } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { routes } from './app.routes';

describe('App routing and shell', () => {
  let fixture: ReturnType<typeof TestBed.createComponent<App>>;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes), provideLocationMocks()],
    }).compileComponents();

    fixture = TestBed.createComponent(App);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  async function navigate(url: string): Promise<HTMLElement> {
    await router.navigateByUrl(url);
    fixture.detectChanges();
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('redirects the root route to the dashboard', async () => {
    await navigate('/');

    expect(router.url).toBe('/dashboard');
  });

  it('renders primary navigation and marks the active route', async () => {
    const page = await navigate('/assets');
    const links = Array.from(page.querySelectorAll('nav a'));

    expect(links.map((link) => link.textContent?.trim())).toEqual([
      'Dashboard',
      'Assets',
      'Alarms',
    ]);
    expect(links.find((link) => link.textContent?.trim() === 'Assets')?.classList).toContain(
      'is-active',
    );
  });

  it.each([
    ['/dashboard', 'Dashboard'],
    ['/alarms', 'Alarms'],
  ])('renders the %s placeholder', async (url, title) => {
    const page = await navigate(url);

    expect(page.querySelector('h1')?.textContent).toContain(title);
    expect(page.textContent).toContain('not complete yet');
  });

  it('renders a not-found page with a dashboard link', async () => {
    const page = await navigate('/not-a-route');
    const dashboardLink = Array.from(
      page.querySelectorAll<HTMLAnchorElement>('a[href="/dashboard"]'),
    ).find((link) => link.textContent?.includes('Return to Dashboard'));

    expect(page.querySelector('h1')?.textContent).toContain('Page not found');
    expect(dashboardLink?.textContent).toContain('Return to Dashboard');

    dashboardLink?.click();
    await fixture.whenStable();
    expect(router.url).toBe('/dashboard');
  });
});
