import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/core/config/app.config';
import { AppRootComponent } from './app/layout/app-root/app-root';

bootstrapApplication(AppRootComponent, appConfig).catch((err) => console.error(err));
