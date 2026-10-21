import { Component, effect, inject, input, signal } from '@angular/core';
import { ImageStoreService } from '../../../core/services/image-store.service';

const FALLBACK = 'images/placeholder.svg';

/** Обложка игры с поддержкой пользовательских изображений и запасной заглушкой. */
@Component({
  selector: 'app-cover-image',
  standalone: true,
  template: `<img
    [src]="resolved()"
    [alt]="alt()"
    [class.cover__img--contain]="fit() === 'contain'"
    loading="lazy"
    (error)="onError()"
  />`,
  styles: [
    `
      :host {
        display: block;
        width: 100%;
        height: 100%;
        overflow: hidden;
        background: #071f35;
      }

      img {
        width: 100%;
        height: 100%;
        object-fit: cover;
        object-position: center;
      }

      .cover__img--contain {
        object-fit: contain;
        padding: 18%;
      }
    `,
  ],
})
export class CoverImageComponent {
  private readonly imageStore = inject(ImageStoreService);
  private requestToken = 0;

  readonly src = input<string | undefined>(undefined);
  readonly alt = input('');
  readonly fit = input<'cover' | 'contain'>('cover');

  protected readonly resolved = signal<string>(FALLBACK);

  constructor() {
    effect(() => {
      const reference = this.src();
      const token = ++this.requestToken;
      if (!reference) {
        this.resolved.set(FALLBACK);
        return;
      }
      void this.imageStore.resolveUrl(reference).then((url) => {
        if (token === this.requestToken) {
          this.resolved.set(url ?? FALLBACK);
        }
      });
    });
  }

  protected onError(): void {
    this.resolved.set(FALLBACK);
  }
}
