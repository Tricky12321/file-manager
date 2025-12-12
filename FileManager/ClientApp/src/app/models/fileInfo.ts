export interface FileInfo {
  isHardlink: boolean;
  path: string;
  inode: string;
  size: number;
  partialHash: string;
  folderPath: string;
  folderName: string;
  inQbit: boolean;
  folderInQbit: boolean;
  sizeMb: number;
  sizeGb: number;
  selected: boolean;
  hashDuplicate: boolean;
  torrentPath: string;
}
