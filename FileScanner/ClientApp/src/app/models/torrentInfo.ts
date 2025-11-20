export interface TorrentInfo {
  hash: string;
  name: string;
  sizeBytes: number;
  progress: number;
  savePath: string;
  tags: string;
  category: string;
  state: string;
  downloadSpeedBytes: number;
  uploadSpeedBytes: number;
  etaSeconds: number;
  contentPath: string;
  totalSizeBytes: number;
  totalSizeGigabytes: number;
}
