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

using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Permissions;
using System.Text;
namespace NTFSDirect
{
	[SuppressUnmanagedCodeSecurity]
	[HostProtection(SecurityAction.LinkDemand, SharedState = true)]
	[Serializable]
	public class Win32Exception : ExternalException, ISerializable
	{
		// Microsoft.Win32.SafeNativeMethods
		[DllImport("kernel32.dll", BestFitMapping = true, CharSet = CharSet.Auto, SetLastError = true)]
		public static extern int FormatMessage(int dwFlags, HandleRef lpSource, int dwMessageId, int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr arguments);

		// Microsoft.Win32.NativeMethods
		public static readonly HandleRef NullHandleRef = new HandleRef(null, IntPtr.Zero);

		private readonly int nativeErrorCode;
		public int NativeErrorCode
		{
			get
			{
				return this.nativeErrorCode;
			}
		}
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public Win32Exception() : this(Marshal.GetLastWin32Error())
		{
		}
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public Win32Exception(int error) : this(error, Win32Exception.GetErrorMessage(error))
		{
		}
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public Win32Exception(int error, string message) : base(message)
		{
			this.nativeErrorCode = error;
		}
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public Win32Exception(string message) : this(Marshal.GetLastWin32Error(), message)
		{
		}
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public Win32Exception(string message, Exception innerException) : base(message, innerException)
		{
			this.nativeErrorCode = Marshal.GetLastWin32Error();
		}
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected Win32Exception(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			//IntSecurity.UnmanagedCode.Demand();
			this.nativeErrorCode = info.GetInt32("NativeErrorCode");
		}
		private static string GetErrorMessage(int error)
		{
			string result = "";
			StringBuilder stringBuilder = new StringBuilder(256);
			int num = FormatMessage(12800, NullHandleRef, error, 0, stringBuilder, stringBuilder.Capacity + 1, IntPtr.Zero);
			if (num != 0)
			{
				int i;
				for (i = stringBuilder.Length; i > 0; i--)
				{
					char c = stringBuilder[i - 1];
					if (c > ' ' && c != '.')
					{
						break;
					}
				}
				result = stringBuilder.ToString(0, i);
			}
			else
			{
				result = "Unknown error (0x" + Convert.ToString(error, 16) + ")";
			}
			return result;
		}
		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException("info");
			}
			info.AddValue("NativeErrorCode", this.nativeErrorCode);
			base.GetObjectData(info, context);
		}
	}
}
