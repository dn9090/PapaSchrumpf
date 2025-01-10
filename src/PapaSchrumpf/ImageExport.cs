using System;

namespace PapaSchrumpf;

public struct ImageExport
{
	public string outputDirectory;

	public string fileNameExtension;

	public bool appendNameExtension;

	public bool keepModificationDate;

	public bool keepCreationDate;

	public bool deleteFiles;
}
