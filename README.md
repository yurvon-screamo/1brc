# 1️⃣🐝🏎️ The One Billion Row Challenge - .NET Edition

1BRC - Safe C# 5.5 sec on i5-1240P 2.1G 12c16t

> The One Billion Row Challenge (1BRC [Original Java Challenge](https://github.com/gunnarmorling/1brc)) is a fun exploration of how far modern .NET can be pushed for aggregating one billion rows from a text file.
> Grab all your (virtual) threads, reach out to SIMD, optimize your GC, or pull any other trick, and create the fastest implementation for solving this task!

## Results

Tested on a i5-1240P 2.1G 12c16t, 16gb, nvme.

* Total: 00:05.57
* Language C#
* Runtime net8/aot/win-x64

## Running

Clone [Original Java Challenge](https://github.com/gunnarmorling/1brc) and generate `measurements.txt`, move file to src directory.

```bash
cd src
dotnet publish -c Release
./bin/Release/net8.0/win-x64/publish/1b.exe
```
