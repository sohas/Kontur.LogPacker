This is a template to use for a Kontur.LogPacker task solution.  
  
Consists of the following projects:

### Kontur.LogPacker

It is my solution for task from SCB Kontur. The essence of the task was to improve compression of big log files in comparison to gzip compressor. It was alowed to use gzip but not alowed to add external components or libraties to the project.

The main idea of the solution is to find big pieces of equal sequences of bytes, then to replace the by simple masks before gzipping.

### Kontur.LogPacker.SelfCheck

A set of simple tests to validate the correctness of your solution. Run with `dotnet run -c Release <project-directory>`, for example `dotnet run -c Release ../Kontur.LogPacker`.

### Kontur.LogPacker.SubmitHelper

An utility that helps you properly pack your solution into a zip archive for submission. The resulting zip file will be placed into the root directory of the solution (the folder where `Kontur.LogPacker.sln` resides).
Run with `dotnet run -c Release` of simply from the IDE.

### Kontur.LogPackerGZip

A simple log archiver that uses GZip. Is used internally by `Kontur.LogPacker.SelfCheck`.
