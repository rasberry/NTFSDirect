NTFSDirect
==========

NTFS MFT (master file table) raw reader and parser

##Usage

```c#
string vol = "c:";
var fileList = new NTFSDirect.Enumerator(vol);
foreach(string file in fileList) {
	FileInfo f = new FileInfo(file);
	if (!f.Exists) { continue; } //every file is enumerated even ones we don't have access to.
	//Do something with each path
}
```

Get only some extensions
```c#
string vol = "c:";
var fileList = new NTFSDirect.Enumerator(vol, new [] {".txt", ".md"});
foreach(string file in fileList) {
	FileInfo f = new FileInfo(file);
	if (!f.Exists) { continue; } //every file is enumerated even ones we don't have access to.
	//Do something with each path
}
```

This code is based on the work found [here](http://code.google.com/p/phever/source/browse/trunk/mft/mftdb/mftdb/CChangeJournal.cs?r=32)
