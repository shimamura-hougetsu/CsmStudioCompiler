# CsmStudioCli
 Command Line Interface to CsmStudio, a program converts ass subtitles to OoM subtitles (clpi+m2ts).
 
## Usage

Download the release executable, copy `MonteCarlo.External.MuxRemoting.dll` and `MuxCommon.DLL` into the same folder as `CsmStudioCli.exe`, then run it in CMD or PowerShell. The `BdMuxServer` is also required, you can copy it to the default location specified in the usage, or point it to your install location. Theese files are not included because they are parts of a commercial software.

The usage is as follows:

```
  -i, --input     Required. Paths to subtitle ass files

  -l, --lang      Languages of subtitles

  -o, --output    Output filename

  -f, --format    (Default: 1080p) Video Format (1080p/1080i/720p/576p/576i/480p/480i)

  -r, --rate      (Default: 23.976) Video Framerate (23.976/24/25/29.97/50/59.94)

  -t, --intime    (Default: 0:10:00.000) InTime Offset, format: XX:XX:XX.XXX

  --server        (Default: .\BdMuxServer\MuxRemotingServer.exe) Path to Mux Server executable

  --schema        (Default: .\BdMuxServer\ProjectSchema) Path to Project Schema

  --temp          (Default: .\Temporary) Path to temporary directory

  --port          (Default: 9920) Path of Mux Server

  --cache         (Default: 838860800) SppfMaxCacheSize

  --help          Display this help screen.

  --version       Display version information.
```

## Return Code

 - 0: Conversion completed successfully.
 - 1: Conversion failed because of an error, please refer to the Mux Server logs for details.
 - -1: Incorrect command line arguments.
 - -2: Invalid information is provided to some options, the reason will be printed.

## Development

The project was developed using Visual Studio 2019 with .NET Framework 4.5.1. You need assemblies from the release and the two aforementioned DLLs to compile.

## License and Copyright Notice

This project is a command line interface to, links to, and includes parts of code from [CsmStudio](https://github.com/subelf/CsmStudio), therefore it's released under GPLv3. To make the release usable, compiled libraries from [CsmStudio](https://github.com/subelf/CsmStudio) and [Spp2Pgs](https://github.com/subelf/Spp2Pgs) are included. The included `ProjectManager.dll` was compiled with [one modification at ProjectCompiler.cs#L477](https://github.com/subelf/CsmStudio/blob/8a0e5eac0b124cf24b6eeac764aeab1719b9e0e1/CsmStudio.ProjectManager/Compile/ProjectCompiler.cs#L477), the `false` was changed to `true` for full ass effects. Other binaries were not modified. For more information about the original projects and the author please visit the link to the repo.


