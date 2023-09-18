using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using Avalonia;
using Avalonia.VisualTree;
using ImageConverterApp.Utility;
using SkiaSharp;
using System;

namespace ImageConverterApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public string Licenses =>
"""
AvaloniaUI/Avalonia(https://github.com/AvaloniaUI/Avalonia/blob/master/licence.md)
---
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors All Rights Reserved

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


mono/SkiaSharp(https://github.com/mono/SkiaSharp/blob/main/LICENSE.md)
---
Copyright (c) 2015-2016 Xamarin, Inc.
Copyright (c) 2017-2018 Microsoft Corporation.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
""";

    [ObservableProperty]
    public ObservableCollection<string> _convertFormats = new ObservableCollection<string>()
    {
        "Jpeg",
        "Png",
        "Webp"
    };
    [ObservableProperty]
    private string _convertFormat = string.Empty;
    [ObservableProperty]
    private int _convertQuality = 100;

    [ObservableProperty]
    public ObservableCollection<IStorageFile> _selectedFiles = new ObservableCollection<IStorageFile>();
    [ObservableProperty]
    private double _runningProgress = 0.0;
    [ObservableProperty]
    private string _runningFile = string.Empty;

    public MainViewModel()
    {
        if(0 < ConvertFormats.Count)
        {
            ConvertFormat = ConvertFormats[0];
        }
    }

    private SKEncodedImageFormat ConvertImageFormat()
        => ConvertFormat switch
        {
            "Png" => SKEncodedImageFormat.Png,
            "Jpeg" => SKEncodedImageFormat.Jpeg,
            "Webp" => SKEncodedImageFormat.Webp,
            _ => throw new NotImplementedException($"NotImpl. : {ConvertFormat}")
        };
    private string ConvertImageExtension()
        => ConvertFormat switch
        {
            "Png" => ".png",
            "Jpeg" => ".jpeg",
            "Webp" => ".webp",
            _ => throw new NotImplementedException($"NotImpl. : {ConvertFormat}")
        };


    [RelayCommand]
    private async Task OnSelectFilesAsync(CancellationToken cancellationToken)
    {
        var level = Application.Current.GetTopLevel();
        if (level != null)
        {
            var files = await level.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions()
            {
                AllowMultiple = true,
                FileTypeFilter = new FilePickerFileType[]
                {
                    FilePickerFileTypes.ImageJpg,
                    FilePickerFileTypes.ImagePng,
                }
            });
            SelectedFiles.Clear();
            foreach (var file in files)
            {
                SelectedFiles.Add(file);
            }
            ConvertFilesCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanConvertFiles))]
    private async Task OnConvertFilesAsync(CancellationToken cancellationToken)
    {
        var level = Application.Current.GetTopLevel();
        if (level != null)
        {
            var dirs = await level.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
            {
                AllowMultiple = false
            });

            if (0 < dirs.Count)
            {
                var dir = dirs[0];
                foreach (var file in SelectedFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var dstPath = System.IO.Path.ChangeExtension(file.Name, ConvertImageExtension());
                    var convertFile = await dir.CreateFileAsync(dstPath);
                    if (convertFile != null)
                    {
                        using var srcFileStream = await file.OpenReadAsync().ConfigureAwait(false);
                        using var convertFileStream = await convertFile.OpenWriteAsync().ConfigureAwait(false);

                        var bitmap = SKBitmap.Decode(srcFileStream);
                        var test = bitmap?.Encode(convertFileStream, ConvertImageFormat(), ConvertQuality);
                    }
                }
            }
        }
    }
    private bool CanConvertFiles()
    {
        return 0 < SelectedFiles.Count;
    }
}
