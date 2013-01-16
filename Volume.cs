//Copyright 2010 Lucemia Chen
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//		
//		http://www.apache.org/licenses/LICENSE-2.0
//		
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace NTFSDirect
{
	public class Volume
	{
		public void EnumerateVolume (string drive, HashSet<string> fileExtensions
			,out Dictionary<ulong, FileEntry> files
			, out Dictionary<ulong, FileEntry> directories
		) {
			directories = new Dictionary<ulong, FileEntry>();
			files = new Dictionary<ulong, FileEntry> ();
			IntPtr medBuffer = IntPtr.Zero;
			IntPtr changeJournalRootHandle = IntPtr.Zero;
			try {
				GetRootFrnEntry (drive, directories);
				GetRootHandle (drive,out changeJournalRootHandle);
				CreateChangeJournal (changeJournalRootHandle);
				SetupMFT_Enum_DataBuffer (ref medBuffer,changeJournalRootHandle);
				EnumerateFiles (medBuffer, ref files, fileExtensions, directories,changeJournalRootHandle);
			} catch (Exception e) {
				Console.WriteLine(e.Message, e);
				Exception innerException = e.InnerException;
				while (innerException != null) {
					Console.WriteLine (innerException.Message, innerException);
					innerException = innerException.InnerException;
				}
				throw new ApplicationException ("Error in EnumerateVolume()", e);
			} finally {
				if (changeJournalRootHandle.ToInt32 () != WinApi.INVALID_HANDLE_VALUE) {
					WinApi.CloseHandle (changeJournalRootHandle);
				}
				if (medBuffer != IntPtr.Zero) {
					Marshal.FreeHGlobal (medBuffer);
				}
			}
		}

		private void GetRootFrnEntry (string drive, Dictionary<ulong, FileEntry> directories)
		{
			string driveRoot = string.Concat ("\\\\.\\", drive);
			driveRoot = string.Concat (driveRoot, Path.DirectorySeparatorChar);
			IntPtr hRoot = WinApi.CreateFile (driveRoot,
				0,
				WinApi.FILE_SHARE_READ | WinApi.FILE_SHARE_WRITE,
				IntPtr.Zero,
				WinApi.OPEN_EXISTING,
				WinApi.FILE_FLAG_BACKUP_SEMANTICS,
				IntPtr.Zero
			);

			if (hRoot.ToInt32 () != WinApi.INVALID_HANDLE_VALUE) {
				WinApi.BY_HANDLE_FILE_INFORMATION fi = new WinApi.BY_HANDLE_FILE_INFORMATION ();
				bool bRtn = WinApi.GetFileInformationByHandle (hRoot, out fi);
				if (bRtn) {
					ulong fileIndexHigh = (ulong)fi.FileIndexHigh;
					ulong indexRoot = (fileIndexHigh << 32) | fi.FileIndexLow;

					FileEntry f = new FileEntry (driveRoot, 0);
					directories.Add (indexRoot, f);
				} else {
					throw new IOException ("GetFileInformationbyHandle() returned invalid handle",
						new Win32Exception (Marshal.GetLastWin32Error ()));
				}
				WinApi.CloseHandle (hRoot);
			} else {
				throw new IOException ("Unable to get root frn entry", new Win32Exception (Marshal.GetLastWin32Error ()));
			}
		}

		private void GetRootHandle (string drive, out IntPtr changeJournalRootHandle)
		{
			string vol = string.Concat ("\\\\.\\", drive);
			changeJournalRootHandle = WinApi.CreateFile (vol,
				WinApi.GENERIC_READ | WinApi.GENERIC_WRITE,
				WinApi.FILE_SHARE_READ | WinApi.FILE_SHARE_WRITE,
				IntPtr.Zero,
				WinApi.OPEN_EXISTING,
				0,
				IntPtr.Zero);
			if (changeJournalRootHandle.ToInt32 () == WinApi.INVALID_HANDLE_VALUE) {
				throw new IOException ("CreateFile() returned invalid handle",
					new Win32Exception (Marshal.GetLastWin32Error ()));
			}
		}

		unsafe private void EnumerateFiles (IntPtr medBuffer
			, ref Dictionary<ulong, FileEntry> files
			, HashSet<string> fileExtensions
			, Dictionary<ulong,FileEntry> directories
			, IntPtr changeJournalRootHandle
		) {
			IntPtr pData = Marshal.AllocHGlobal (sizeof(ulong) + 0x10000);
			WinApi.ZeroMemory (pData, sizeof(ulong) + 0x10000);
			uint outBytesReturned = 0;

			while (false != WinApi.DeviceIoControl(changeJournalRootHandle, WinApi.FSCTL_ENUM_USN_DATA, medBuffer,
				sizeof(WinApi.MFT_ENUM_DATA), pData, sizeof(ulong) + 0x10000, out outBytesReturned,
				IntPtr.Zero))
			{
				IntPtr pUsnRecord = new IntPtr (pData.ToInt32 () + sizeof(Int64));
				while (outBytesReturned > 60) {
					WinApi.USN_RECORD usn = new WinApi.USN_RECORD (pUsnRecord);

					if (0 != (usn.FileAttributes & WinApi.FILE_ATTRIBUTE_DIRECTORY)) {
						//
						// handle directories
						//
						if (!directories.ContainsKey (usn.FileReferenceNumber)) {
							directories.Add (usn.FileReferenceNumber,
							new FileEntry (usn.FileName, usn.ParentFileReferenceNumber));
						} else {
							// this is debug code and should be removed when we are certain that
							// duplicate frn's don't exist on a given drive.  To date, this exception has
							// never been thrown.  Removing this code improves performance....
							throw new Exception (string.Format ("Duplicate FRN: {0} for {1}",
								usn.FileReferenceNumber, usn.FileName)
							);
						}
					} else {
						//
						// handle files
						//
						bool add = true;
						if (fileExtensions != null) {
							string s = Path.GetExtension (usn.FileName);
							add = fileExtensions.Contains(s);
						}
						if (add) {
							if (!files.ContainsKey (usn.FileReferenceNumber)) {
								files.Add (usn.FileReferenceNumber,
								new FileEntry (usn.FileName, usn.ParentFileReferenceNumber));
							} else {
								FileEntry frn = files [usn.FileReferenceNumber];
								if (0 != string.Compare (usn.FileName, frn.Name, true)) {
									Console.WriteLine(
										"Attempt to add duplicate file reference number: {0} for file {1}, file from index {2}",
										usn.FileReferenceNumber, usn.FileName, frn.Name);
									throw new Exception (string.Format ("Duplicate FRN: {0} for {1}",
										usn.FileReferenceNumber, usn.FileName)
									);
								}
							}
						}
					}
					pUsnRecord = new IntPtr (pUsnRecord.ToInt32 () + usn.RecordLength);
					outBytesReturned -= usn.RecordLength;
				}
				Marshal.WriteInt64 (medBuffer, Marshal.ReadInt64 (pData, 0));
			}
			Marshal.FreeHGlobal (pData);
		}

		unsafe private void CreateChangeJournal (IntPtr changeJournalRootHandle)
		{
			// This function creates a journal on the volume. If a journal already
			// exists this function will adjust the MaximumSize and AllocationDelta
			// parameters of the journal
			ulong MaximumSize = 0x800000;
			ulong AllocationDelta = 0x100000;
			uint cb;
			WinApi.CREATE_USN_JOURNAL_DATA cujd;
			cujd.MaximumSize = MaximumSize;
			cujd.AllocationDelta = AllocationDelta;

			int sizeCujd = Marshal.SizeOf (cujd);
			IntPtr cujdBuffer = Marshal.AllocHGlobal (sizeCujd);
			WinApi.ZeroMemory (cujdBuffer, sizeCujd);
			Marshal.StructureToPtr (cujd, cujdBuffer, true);

			bool fOk = WinApi.DeviceIoControl (changeJournalRootHandle, WinApi.FSCTL_CREATE_USN_JOURNAL,
				cujdBuffer, sizeCujd, IntPtr.Zero, 0, out cb, IntPtr.Zero);
			if (!fOk) {
				throw new IOException ("DeviceIoControl() returned false", new Win32Exception (Marshal.GetLastWin32Error ()));
			}
		}

		unsafe private void SetupMFT_Enum_DataBuffer (ref IntPtr medBuffer, IntPtr changeJournalRootHandle)
		{
			uint bytesReturned = 0;
			WinApi.USN_JOURNAL_DATA ujd = new WinApi.USN_JOURNAL_DATA ();

			bool bOk = WinApi.DeviceIoControl (
				changeJournalRootHandle,                           // Handle to drive
				WinApi.FSCTL_QUERY_USN_JOURNAL,   // IO Control Code
				IntPtr.Zero,                // In Buffer
				0,                          // In Buffer Size
				out ujd,                    // Out Buffer
				sizeof(WinApi.USN_JOURNAL_DATA),  // Size Of Out Buffer
				out bytesReturned,          // Bytes Returned
				IntPtr.Zero               // lpOverlapped
			);
			if (bOk) {
				WinApi.MFT_ENUM_DATA med;
				med.StartFileReferenceNumber = 0;
				med.LowUsn = 0;
				med.HighUsn = ujd.NextUsn;
				int sizeMftEnumData = Marshal.SizeOf (med);
				medBuffer = Marshal.AllocHGlobal (sizeMftEnumData);
				WinApi.ZeroMemory (medBuffer, sizeMftEnumData);
				Marshal.StructureToPtr (med, medBuffer, true);
			} else {
				throw new IOException ("DeviceIoControl() returned false", new Win32Exception (Marshal.GetLastWin32Error ()));
			}
		}
	}
}