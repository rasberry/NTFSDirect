NTFSDirect
==========

NTFS MFT (master file table) raw reader and parser

##Usage

```c#
string vol = "c:\";
var fileList = new NTFSDirect.Enumerator(vol);
foreach(string file in fileList) {
	FileInfo f = new FileInfo(file);
	if (!f.Exists) { continue; } //every file is enumerated even ones we might not have access to.
	//Do something with each path
}
```

