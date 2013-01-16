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
using System.Runtime.InteropServices;

namespace NTFSDirect
{
	public class WinApi
	{
		public const UInt32 GENERIC_READ = 0x80000000;
		public const UInt32 GENERIC_WRITE = 0x40000000;
		public const UInt32 FILE_SHARE_READ = 0x00000001;
		public const UInt32 FILE_SHARE_WRITE = 0x00000002;
		public const UInt32 FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
		public const UInt32 OPEN_EXISTING = 3;
		public const UInt32 FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
		public const Int32 INVALID_HANDLE_VALUE = -1;
		public const UInt32 FSCTL_QUERY_USN_JOURNAL = 0x000900f4;
		public const UInt32 FSCTL_ENUM_USN_DATA = 0x000900b3;
		public const UInt32 FSCTL_CREATE_USN_JOURNAL = 0x000900e7;

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr CreateFile (string lpFileName, uint dwDesiredAccess,
			uint dwShareMode, IntPtr lpSecurityAttributes,
			uint dwCreationDisposition, uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetFileInformationByHandle (IntPtr hFile,
			out BY_HANDLE_FILE_INFORMATION lpFileInformation);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle (IntPtr hObject);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeviceIoControl (IntPtr hDevice,
			UInt32 dwIoControlCode,
			IntPtr lpInBuffer, Int32 nInBufferSize,
			out USN_JOURNAL_DATA lpOutBuffer, Int32 nOutBufferSize,
			out uint lpBytesReturned, IntPtr lpOverlapped);

		[DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeviceIoControl (IntPtr hDevice,
			UInt32 dwIoControlCode,
			IntPtr lpInBuffer, Int32 nInBufferSize,
			IntPtr lpOutBuffer, Int32 nOutBufferSize,
			out uint lpBytesReturned, IntPtr lpOverlapped);

		[DllImport("kernel32.dll")]
		public static extern void ZeroMemory (IntPtr ptr, Int32 size);

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct BY_HANDLE_FILE_INFORMATION
		{
			public uint FileAttributes;
			public FILETIME CreationTime;
			public FILETIME LastAccessTime;
			public FILETIME LastWriteTime;
			public uint VolumeSerialNumber;
			public uint FileSizeHigh;
			public uint FileSizeLow;
			public uint NumberOfLinks;
			public uint FileIndexHigh;
			public uint FileIndexLow;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct FILETIME
		{
			public uint DateTimeLow;
			public uint DateTimeHigh;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct USN_JOURNAL_DATA
		{
			public UInt64 UsnJournalID;
			public Int64 FirstUsn;
			public Int64 NextUsn;
			public Int64 LowestValidUsn;
			public Int64 MaxUsn;
			public UInt64 MaximumSize;
			public UInt64 AllocationDelta;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct MFT_ENUM_DATA
		{
			public UInt64 StartFileReferenceNumber;
			public Int64 LowUsn;
			public Int64 HighUsn;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CREATE_USN_JOURNAL_DATA
		{
			public UInt64 MaximumSize;
			public UInt64 AllocationDelta;
		}

		public class USN_RECORD
		{
			public UInt32 RecordLength;
			public UInt64 FileReferenceNumber;
			public UInt64 ParentFileReferenceNumber;
			public UInt32 FileAttributes;
			public Int32 FileNameLength;
			public Int32 FileNameOffset;
			public string FileName = string.Empty;
			private const int FR_OFFSET = 8;
			private const int PFR_OFFSET = 16;
			private const int FA_OFFSET = 52;
			private const int FNL_OFFSET = 56;
			private const int FN_OFFSET = 58;

			public USN_RECORD (IntPtr p)
			{
				this.RecordLength = (UInt32)Marshal.ReadInt32 (p);
				this.FileReferenceNumber = (UInt64)Marshal.ReadInt64 (p, FR_OFFSET);
				this.ParentFileReferenceNumber = (UInt64)Marshal.ReadInt64 (p, PFR_OFFSET);
				this.FileAttributes = (UInt32)Marshal.ReadInt32 (p, FA_OFFSET);
				this.FileNameLength = Marshal.ReadInt16 (p, FNL_OFFSET);
				this.FileNameOffset = Marshal.ReadInt16 (p, FN_OFFSET);
				FileName = Marshal.PtrToStringUni (new IntPtr (p.ToInt32 () + this.FileNameOffset), this.FileNameLength / sizeof(char));
			}
		}
	}
}

