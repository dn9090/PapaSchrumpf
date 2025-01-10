using System;

namespace PapaSchrumpf;

public enum ImageUnit
{
	Pixel,
	Percentage
}

public enum ImageResizeMode
{
	None,
	Min,
	Max,
	Stretch,
	Crop
}

public enum ImageFormat
{
	Auto = 0,
	Png  = 1,
	Jpeg = 2,
}

public enum ImageFilter
{
	None         = 0,
	Grayscale    = 1 << 0,
	HighContrast = 1 << 1,
	Sharpness    = 1 << 2,
}

public enum ImageResolution
{
	A8,
	A7,
	HD,
	FullHD
}

public struct ImageOptions
{
	public ImageFormat format;

	public ImageResizeMode resizeMode;

	public int width;

	public int height;

	public ImageUnit unit;

	public bool percentage;

	public float quality;

	public ImageFilter filter;

	public float constrast;

	public float sharpness;

	public bool clearExif;

	public void SetResolution(ImageResolution resolution)
	{
		unit = ImageUnit.Pixel;

		switch(resolution)
		{
			case ImageResolution.A8:
				width  = 874;
				height = 614;
				break;
			case ImageResolution.A7:
				width  = 1240;
				height = 874;
				break;
			case ImageResolution.HD:
				width  = 1280;
				height = 720;
				break;
			case ImageResolution.FullHD:
				width  = 1920;
				height = 1080;
				break;
		}
	}

	public bool IsResolution(ImageResolution resolution)
	{
		if(unit == ImageUnit.Pixel)
		{
			switch(resolution)
			{
				case ImageResolution.A8:     return width == 874 && height == 614;
				case ImageResolution.A7:     return width == 1240 && height == 874;
				case ImageResolution.HD:     return width == 1280 && height == 720;
				case ImageResolution.FullHD: return width == 1920 && height == 1080;
			}
		}
		
		return false;
	}
}