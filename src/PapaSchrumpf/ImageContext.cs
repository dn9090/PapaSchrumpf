using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PapaSchrumpf;

public class ImageContext : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;

	public ImageOptions options;

	public ImageExport export;

	public ImageList Files { get; }

	public int Unit
	{
		get => (int)options.unit;
		set
		{
			options.unit = (ImageUnit)value;
			NotifyPropertyChanged("");
		}
	}

	public bool IsPixel => options.unit == ImageUnit.Pixel;

	public string[] Formats => Enum.GetNames<ImageFormat>();

	public string Format
	{
		get => options.format.ToString();
		set
		{
			options.format = Enum.Parse<ImageFormat>(value);
			NotifyPropertyChanged();
		}
	}

	public bool IsAspect
	{
		get => AspectMin || AspectMax;
		set
		{
			if(value)
			{
				if(!AspectMin && !AspectMax)
					AspectMax = true;
			} else {
				options.resizeMode = ImageResizeMode.Stretch;
			}

			NotifyPropertyChanged();
		}
	}

	public bool AspectMin
	{
		get => options.resizeMode == ImageResizeMode.Min;
		set
		{
			if(value)
			{
				options.resizeMode = ImageResizeMode.Min;
				NotifyPropertyChanged();
			}
		}
	}

	public bool AspectMax
	{
		get => options.resizeMode == ImageResizeMode.Max;
		set
		{
			if(value)
			{
				options.resizeMode = ImageResizeMode.Max;
				NotifyPropertyChanged();
			}
		}
	}

	public string Width
	{
		get => options.width > 0 ? options.width.ToString() : string.Empty;
		set
		{
			if(int.TryParse(value, out var integer))
			{
				options.width = Math.Max(integer, 0);
				NotifyPropertyChanged("");
			}
		}
	}

	public string Height
	{
		get => options.height > 0 ? options.height.ToString() : string.Empty;
		set
		{
			if(int.TryParse(value, out var integer))
			{
				options.height = Math.Max(integer, 0);
				NotifyPropertyChanged("");
			}
		}
	}

	public bool IsA8
	{
		get => options.IsResolution(ImageResolution.A8);
		set
		{
			options.SetResolution(ImageResolution.A8);
			NotifyPropertyChanged("");
		}
	}

	public bool IsA7
	{
		get => options.IsResolution(ImageResolution.A7);
		set
		{
			options.SetResolution(ImageResolution.A7);
			NotifyPropertyChanged("");
		}
	}

	public bool IsHD
	{
		get => options.IsResolution(ImageResolution.HD);
		set
		{
			options.SetResolution(ImageResolution.HD);
			NotifyPropertyChanged("");
		}
	}

	public bool IsFullHD
	{
		get => options.IsResolution(ImageResolution.FullHD);
		set
		{
			options.SetResolution(ImageResolution.FullHD);
			NotifyPropertyChanged("");
		}
	}

	public double Quality
	{
		get => Math.Round(options.quality * 100f);
		set
		{
			options.quality = (float)value / 100f;
			NotifyPropertyChanged();
		}
	}

	public bool IsGrayscale
	{
		get => options.filter.HasFlag(ImageFilter.Grayscale);
		set
		{
			if(value)
				options.filter |= ImageFilter.Grayscale;
			else
				options.filter &= ~ImageFilter.Grayscale;
			NotifyPropertyChanged();
		}
	}

	public bool IsHighContrast
	{
		get => options.filter.HasFlag(ImageFilter.HighContrast);
		set
		{
			if(value)
				options.filter |= ImageFilter.HighContrast;
			else
				options.filter &= ~ImageFilter.HighContrast;
			NotifyPropertyChanged();
		}
	}

	public double Contrast
	{
		get => Math.Round(options.constrast * 100f);
		set
		{
			options.constrast = (float)value / 100f;
			NotifyPropertyChanged();
		}
	}

	public bool IsSharpening
	{
		get => options.filter.HasFlag(ImageFilter.Sharpness);
		set
		{
			if(value)
				options.filter |= ImageFilter.Sharpness;
			else
				options.filter &= ~ImageFilter.Sharpness;
			NotifyPropertyChanged();
		}
	}

	public double Sharpness
	{
		get => Math.Round(options.sharpness * 100f);
		set
		{
			options.sharpness = (float)value / 100f;
			NotifyPropertyChanged();
		}
	}

	public bool IsCustomFolder
	{
		get => export.outputDirectory != null;
		set
		{
			if(value)
				export.outputDirectory = CustomFolderPath;
			else
				export.outputDirectory = null;
			
			NotifyPropertyChanged();
		}
	}

	public string CustomFolderPath
	{
		get => customFolderPath;
		set
		{
			customFolderPath = value;

			if(IsCustomFolder)
				export.outputDirectory = value;
			
			NotifyPropertyChanged();
		}
	}

	internal string customFolderPath;

	public bool IsCustomExtension
	{
		get => export.fileNameExtension != null;
		set
		{
			if(value)
				export.fileNameExtension = CustomExtension;
			else
				export.fileNameExtension = null;
			NotifyPropertyChanged();
		}
	}

	public string CustomExtension
	{
		get => customExtension;
		set
		{
			customExtension = value;

			if(IsCustomExtension)
				export.fileNameExtension = value;

			NotifyPropertyChanged();
		}
	}

	internal string customExtension;

	public bool AppendExtension
	{
		get => export.appendNameExtension;
		set
		{
			export.appendNameExtension = value;
			NotifyPropertyChanged();
		}
	}

	public bool PrependExtension
	{
		get => !export.appendNameExtension;
		set
		{
			export.appendNameExtension = !value;
			NotifyPropertyChanged();
		}
	}

	public bool ClearExif
	{
		get => options.clearExif;
		set
		{
			options.clearExif = value;
			NotifyPropertyChanged();
		}
	}

	public bool CopyCreationDate
	{
		get => export.keepCreationDate;
		set
		{
			export.keepCreationDate = value;
			NotifyPropertyChanged();
		}
	}

	public bool CopyModificationDate
	{
		get => export.keepModificationDate;
		set
		{
			export.keepModificationDate = value;
			NotifyPropertyChanged();
		}
	}

	public bool DeleteFiles
	{
		get => export.deleteFiles;
		set
		{
			export.deleteFiles = value;
			NotifyPropertyChanged();
		}
	}

	public string LastFilePath { get; set; }

	public ImageContext()
	{
		Files             = new ImageList();
		IsAspect          = true;
		Quality           = 90;
		Contrast          = 50;
		Sharpness         = 50;
		IsCustomFolder    = false;
		CustomFolderPath  = string.Empty;
		IsCustomExtension = false;
		CustomExtension   = string.Empty;
		PrependExtension  = true;
		CopyCreationDate  = true;
	}

	internal void NotifyPropertyChanged([CallerMemberName] string propertyName = "")  
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}