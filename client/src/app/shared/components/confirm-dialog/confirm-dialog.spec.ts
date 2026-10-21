import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmDialogComponent } from './confirm-dialog';

describe('ConfirmDialogComponent', () => {
  let fixture: ComponentFixture<ConfirmDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [ConfirmDialogComponent] }).compileComponents();
    fixture = TestBed.createComponent(ConfirmDialogComponent);
  });

  it('не отображает диалог, когда visible = false', () => {
    fixture.componentRef.setInput('visible', false);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.dialog')).toBeNull();
  });

  it('эмитит confirmed при подтверждении', () => {
    fixture.componentRef.setInput('visible', true);
    fixture.componentRef.setInput('title', 'Удалить?');
    fixture.detectChanges();

    let confirmed = false;
    fixture.componentInstance.confirmed.subscribe(() => (confirmed = true));

    const button = fixture.nativeElement.querySelector('.gt-btn--danger') as HTMLButtonElement;
    expect(button).toBeTruthy();
    button.click();
    expect(confirmed).toBeTrue();
  });

  it('эмитит cancelled при отмене', () => {
    fixture.componentRef.setInput('visible', true);
    fixture.detectChanges();

    let cancelled = false;
    fixture.componentInstance.cancelled.subscribe(() => (cancelled = true));

    const button = fixture.nativeElement.querySelector('.gt-btn--ghost') as HTMLButtonElement;
    button.click();
    expect(cancelled).toBeTrue();
  });
});
