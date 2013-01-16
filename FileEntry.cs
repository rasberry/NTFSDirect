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

namespace NTFSDirect
{
	public class FileEntry
	{
		private string _name;

		public string Name {
			get { return _name; }
			set { _name = value; }
		}

		private ulong _parentFrn;

		public UInt64 ParentFrn {
			get { return _parentFrn; }
			set { _parentFrn = value; }
		}

		public FileEntry (string name, ulong parentFrn)
		{
			if (name != null && name.Length > 0) {
				_name = name;
			} else {
				throw new ArgumentException ("Invalid argument: null or Length = zero", "name");
			}
			if (parentFrn >= 0) {
				_parentFrn = parentFrn;
			} else {
				throw new ArgumentException ("Invalid argument: less than zero", "parentFrn");
			}
		}
	}
}

