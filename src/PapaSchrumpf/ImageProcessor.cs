using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.VisualBasic.FileIO;

namespace PapaSchrumpf;

public class ImageProcessor
{
	public int count => m_OutputPaths.Length;

	public int processedCount => Volatile.Read(ref m_Done);

	public bool isDone => Volatile.Read(ref m_Done) >= m_OutputPaths.Length;

	internal string[] m_InputPaths;

	internal string[] m_OutputPaths;

	internal Thread[] m_Threads;

	internal int m_Count;

	internal int m_Done;

	internal CancellationTokenSource m_CancellationSource;
	
	public ImageProcessor(string[] filePaths, int workerCount = -1)
	{
		if(workerCount < 0)
			workerCount = Math.Max(Environment.ProcessorCount / 2, 1);

		m_InputPaths  = filePaths;
		m_OutputPaths = new string[filePaths.Length];
		m_Threads     = new Thread[Math.Min(workerCount, filePaths.Length)];
	}

	public void Start(ImageOptions options, ImageExport export)
	{
		Wait();
		
		m_CancellationSource = new CancellationTokenSource();
		m_Count              = 0;
		m_Done               = 0;
		
		for(int i = 0; i < m_Threads.Length; ++i)
		{
			m_Threads[i] = new Thread(() => Run(options, export));
			m_Threads[i].Start();
		}
	}

	public void Wait()
	{
		for(int i = 0; i < m_Threads.Length; ++i)
		{
			if(m_Threads[i] != null)
				m_Threads[i].Join();
		}
	}

	public void Stop()
	{
		if(m_CancellationSource != null)
			m_CancellationSource.Cancel();
	}

	internal void Run(ImageOptions options, ImageExport export)
	{
		while(!m_CancellationSource.IsCancellationRequested)
		{
			var index = Interlocked.Increment(ref m_Count) - 1;

			if(index >= m_InputPaths.Length)
				break;

			m_OutputPaths[index] = Run(m_InputPaths[index], options, export);

			Interlocked.Increment(ref m_Done);
		}
	}

	internal string Run(string filePath, ImageOptions options, ImageExport export)
	{
		try
		{
			var fileInfo    = new FileInfo(filePath);
			var image       = Image.Load(filePath);
			var imageFormat = image.Metadata.DecodedImageFormat;

			Resize(image, options);
			Postprocess(image, options);

			if(options.clearExif)
			{
				image.Metadata.ExifProfile = null;
				image.Metadata.XmpProfile  = null;
			}

			IImageFormat targetFormat = PngFormat.Instance;

			switch(options.format)
			{
				case ImageFormat.Auto:
				{
					if(imageFormat is PngFormat || imageFormat is JpegFormat)
						targetFormat = imageFormat;
				} break;
				case ImageFormat.Jpeg:
				{
					targetFormat = JpegFormat.Instance;
				} break;
				case ImageFormat.Png:
				{
					targetFormat = PngFormat.Instance;
				} break;
			}

			var fileName = GetFileName(fileInfo.Name, targetFormat.FileExtensions.First(),
				export.fileNameExtension, export.appendNameExtension);
			var customPath = Path.Combine(fileInfo.DirectoryName, fileName);

			if(!string.IsNullOrEmpty(export.outputDirectory))
			{
				Directory.CreateDirectory(export.outputDirectory);
				customPath = Path.Combine(export.outputDirectory, fileName);
			}

			var exportPath = GetUniqueFilePath(customPath);

			if(targetFormat is PngFormat)
			{
				var metadata = image.Metadata.GetPngMetadata();

				var encoder = new PngEncoder() {
					ColorType = options.filter == ImageFilter.Grayscale ? PngColorType.GrayscaleWithAlpha : metadata.ColorType, 
					CompressionLevel = (PngCompressionLevel)(int)Math.Round(float.Lerp(9f, 0f, options.quality))
				};

				image.SaveAsPng(exportPath, encoder);
			} else if(targetFormat is JpegFormat) {
				var metadata = image.Metadata.GetJpegMetadata();

				var encoder = new JpegEncoder() {
					ColorType = options.filter == ImageFilter.Grayscale ? JpegEncodingColor.Luminance : metadata.ColorType,
					Quality   = (int)Math.Round(float.Lerp(1f, 100f, options.quality))
				};

				image.SaveAsJpeg(exportPath, encoder);
			}

			if(export.keepModificationDate)
				File.SetLastWriteTime(exportPath, fileInfo.CreationTime);

			if(export.keepCreationDate)
				File.SetCreationTime(exportPath, fileInfo.CreationTime);

			if(export.deleteFiles)
				FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

			return exportPath;
		} catch(Exception e) {
#if DEBUG
			throw e;
#else
			return null;
#endif
		}
	}

	internal static string GetFileName(string fileName, string fileExtension, string nameExtension, bool append)
	{
		var name = Path.GetFileNameWithoutExtension(fileName);

		if(string.IsNullOrEmpty(fileExtension))
			fileExtension = Path.GetExtension(fileName);
		
		if(!fileExtension.StartsWith('.'))
			fileExtension = "." + fileExtension;

		if(!string.IsNullOrEmpty(nameExtension))
		{
			if(append)
				name = name + nameExtension;
			else
				name = nameExtension + name;
		}
		
		return name + fileExtension;
	}

	internal static string GetUniqueFilePath(string filePath)
	{
		var directory     = Path.GetDirectoryName(filePath); 
		var fileName      = Path.GetFileNameWithoutExtension(filePath);
		var fileExtension = Path.GetExtension(filePath);

		var uniqueName = Path.GetFileName(filePath);
		var count      = 1;

		while(File.Exists(Path.Combine(directory, uniqueName)))
			uniqueName = $"{fileName} {count++}{fileExtension}";

		return Path.Combine(directory, uniqueName);
	}

	internal static void Resize(Image image, ImageOptions options)
	{
		var height = image.Height;
		var width  = image.Width;

		switch(options.unit)
		{
			case ImageUnit.Pixel:
			{
				if(options.width > 0)
					width = options.width;

				if(options.height > 0)
					height = options.height;
			} break;
			case ImageUnit.Percentage:
			{
				if(options.width > 0)
					width = (int)Math.Round(image.Width * (double)options.width / 100.0);

				if(options.height > 0)
					height = (int)Math.Round(image.Height * (double)options.height / 100.0);
			} break;
		}

		if(width != image.Width || height != image.Height)
		{
			switch(options.resizeMode)
			{
				case ImageResizeMode.Min:
				case ImageResizeMode.Max:
				{
					var resizeOptions = new ResizeOptions()
					{
						Size = new Size(width, height),
						Mode = options.resizeMode == ImageResizeMode.Min ? ResizeMode.Min : ResizeMode.Max,
					};

					image.Mutate(x => x.Resize(resizeOptions));
				} break;
				case ImageResizeMode.Stretch:
				{
					image.Mutate(x => x.Resize(width, height));
				} break;
				case ImageResizeMode.Crop:
				{
					var resizeOptions = new ResizeOptions()
					{
						Size = new Size(width, height),
						Mode = ResizeMode.Crop,
					};

					image.Mutate(x => x.Resize(resizeOptions));
				} break;
			}
		}  
	}

	internal static void Postprocess(Image image, ImageOptions options)
	{
		if(options.filter.HasFlag(ImageFilter.HighContrast))
			image.Mutate(x => x.Contrast(1f + 1f * options.constrast));

		if(options.filter.HasFlag(ImageFilter.Grayscale))
			image.Mutate(x => x.Grayscale());

		if(options.filter.HasFlag(ImageFilter.Sharpness))
			image.Mutate(x => x.GaussianSharpen(0.1f + 2f * options.sharpness));
	}
}
