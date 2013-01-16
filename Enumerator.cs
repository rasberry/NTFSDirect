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
using System.IO;

namespace NTFSDirect
{
	public class Enumerator : IEnumerable<string>
	{
		public Enumerator(string volume) //volume is "c:" format (no quotes)
		{
			_volume = volume;
		}
		private string _volume;

		public int Count { get {
			Init();
			return _files.Values.Count;
		}}

		private Dictionary<ulong, NTFSDirect.FileEntry> _files = null;
		private Dictionary<ulong, NTFSDirect.FileEntry> _folders = null;

		private void Init()
		{
			if (_files != null) { return; }
			var ntfs = new NTFSDirect.Volume();
			ntfs.EnumerateVolume(_volume,null,out _files,out _folders);
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public IEnumerator<string> GetEnumerator()
		{
			Init();
			List<string> path = new List<string>(); //path fragment collector
			foreach(NTFSDirect.FileEntry f in _files.Values)
			{
				path.Clear();
				NTFSDirect.FileEntry p = f;
				int dp = -1; //max path length counter to avoid inifinte loops

				do {
					if (p.ParentFrn != 0) {
						path.Add(p.Name);
					}
					if (_files.ContainsKey(p.ParentFrn)) {
						p = _files[p.ParentFrn];
					} else if (_folders.ContainsKey(p.ParentFrn)) {
						p = _folders[p.ParentFrn];
					} else {
						p = null;
					}
				} while(p != null && ++dp < 1000);

				if (path.Count != 0)
				{
					path.Reverse();
					string file = _volume+'\\'+Path.Combine(path.ToArray());
					yield return file;
				}
			}
		}
	}
}

