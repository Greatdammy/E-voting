import * as signalR from '@microsoft/signalr';
import { INTEGRITY_HUB_URL } from '../config';
import { store } from '../store/store';

export function createIntegrityConnection() {
  return new signalR.HubConnectionBuilder()
    .withUrl(INTEGRITY_HUB_URL, {
      accessTokenFactory: () => store.getState().auth.token
    })
    .withAutomaticReconnect()
    .build();
}
