using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using SixLabors.ImageSharp;

namespace PapaSchrumpf;

// https://en.wikipedia.org/wiki/Image_resolution

public partial class MainWindow : Window
{
	public ImageContext Context { get; }

	public MainWindow()
	{
		Context = new ImageContext();

		InitializeTranslator();
		InitializeComponent();

		DataContext = Context;
	}

	internal void InitializeTranslator()
	{
		var en = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/en.json"));
		Translator.Load(en.Stream);

		var de = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/de.json"));
		Translator.Load(de.Stream);
	}

	internal void OnDragEnterFile(object sender, DragEventArgs e)
	{
		if(e.Data.GetDataPresent(DataFormats.FileDrop))
			e.Effects = DragDropEffects.Move;
	}

	internal void OnDropFile(object sender, DragEventArgs e)
	{
		var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);

		foreach(var filePath in filePaths)
		{
			if(Directory.Exists(filePath))
			{
				foreach(var subFilePath in Directory.GetFiles(filePath))
					AddImage(subFilePath);
			} else {
				AddImage(filePath);
			}
		}
	}

	internal void AddImage(string path)
	{
		try
		{
			var fileInfo  = new FileInfo(path);
			var imageInfo = Image.Identify(path);
			
			if(imageInfo != null)
			{
				Context.Files.Add(new ImageInfo {
					Filename   = fileInfo.Name,
					Path       = fileInfo.FullName,
					Resolution = $"{imageInfo.Width} x {imageInfo.Height}"
				});
			}
		} catch {
		}
	}

	internal void OnOpenFile(object sender, RoutedEventArgs e)
	{
		var dialog = new OpenFileDialog()
		{
			InitialDirectory = string.IsNullOrEmpty(Context.LastFilePath)
				? Environment.GetFolderPath(Environment.SpecialFolder.Favorites)
				: Path.GetDirectoryName(Context.LastFilePath),
			Multiselect      = true
		};

		if(dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
		{
			foreach(var filePath in dialog.FileNames)
				AddImage(filePath);

			Context.LastFilePath = dialog.FileNames[dialog.FileNames.Length - 1];
		}
	}

	internal void OnRemoveFile(object sender, RoutedEventArgs e)
	{
		var infos = new ImageInfo[FileView.SelectedItems.Count];
		
		for(int i = 0; i < FileView.SelectedItems.Count; ++i)
			infos[i] = (ImageInfo)FileView.SelectedItems[i];

		foreach(ImageInfo info in infos)
			Context.Files.RemovePath(info.Path);
	}

	internal void OnCustomFolder(object sender, RoutedEventArgs e)
	{
		var dialog = new OpenFolderDialog()
		{
			InitialDirectory = string.IsNullOrEmpty(Context.CustomFolderPath)
				? Environment.GetFolderPath(Environment.SpecialFolder.Favorites)
				: Context.CustomFolderPath,
			Multiselect      = false
		};

		if(dialog.ShowDialog() == true && !string.IsNullOrEmpty(dialog.FolderName))
			Context.CustomFolderPath = dialog.FolderName;
	}

	internal void OnRunAndClear(object sender, RoutedEventArgs e)
	{
		Run(clear: true);
	}

	internal void OnRun(object sender, RoutedEventArgs e)
	{
		Run();
	}

	internal void Run(bool clear = false)
	{
		var processor = new ImageProcessor(Context.Files.GetPaths());
		var options   = Context.options;
		var export    = Context.export;

		var worker = new BackgroundWorker();
		worker.DoWork += (sender, e) => {
			var bw = sender as BackgroundWorker;

			processor.Start(options, export);

			while(!processor.isDone)
			{
				Thread.Sleep(100);
				bw.ReportProgress((int)Math.Round((float)processor.processedCount / processor.count * 100f));
			}

			processor.Wait();
		};
		worker.WorkerReportsProgress = true;
		worker.ProgressChanged += (sender, e) => {
			PBar.Value = e.ProgressPercentage;
		};
		worker.RunWorkerCompleted += (sender, e) => {
			PBar.Value     = 0;
			ABar.IsEnabled = true;

			if(clear)
				Context.Files.Clear();
		};
		worker.RunWorkerAsync();
	
		ABar.IsEnabled = false;
	}
}