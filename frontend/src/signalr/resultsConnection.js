import * as signalR from '@microsoft/signalr';
import { SIGNALR_HUB_URL } from '../config';

export function createResultsConnection() {
  return new signalR.HubConnectionBuilder()
    .withUrl(SIGNALR_HUB_URL, { withCredentials: false })
    .withAutomaticReconnect()
    .build();
}
