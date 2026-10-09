export type NotificationKind = 'success' | 'error' | 'info';

export interface AppNotification {
  id: string;
  kind: NotificationKind;
  message: string;
}
