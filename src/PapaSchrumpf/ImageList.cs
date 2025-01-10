using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PapaSchrumpf;

public struct ImageInfo
{
	public string Filename { get; set; }

	public string Path { get; set; }

	public string Resolution { get; set; }
}

public class ImageList : ObservableCollection<ImageInfo>
{
	internal HashSet<string> paths;

	public ImageList()
	{
		paths = new HashSet<string>();
	}

	protected override void InsertItem(int index, ImageInfo item)
	{
		if(!paths.Contains(item.Path))
		{
			paths.Add(item.Path);
			base.InsertItem(index, item);
		}
	}

	protected override void RemoveItem(int index)
	{
		paths.Remove(Items[index].Path);
		base.RemoveItem(index);
	}

	protected override void ClearItems()
	{
		paths.Clear();
		base.ClearItems();
	}

	public void RemovePath(string path)
	{
		if(paths.Contains(path))
		{
			for(int i = 0; i < Items.Count; ++i)
			{
				if(Items[i].Path == path)
				{
					RemoveAt(i);
					break;
				}
			}
		}
	}

	public string[] GetPaths()
	{
		var array = new string[paths.Count];
		paths.CopyTo(array, 0);

		return array;
	}
}