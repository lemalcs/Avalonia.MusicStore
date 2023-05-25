using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.MusicStore.Models;
using ReactiveUI;

namespace Avalonia.MusicStore.ViewModels;

public class AlbumViewModel: ViewModelBase
{
    /// <summary>
    /// Album information
    /// </summary>
    private readonly Album _album;

    public AlbumViewModel(Album album)
    {
        _album = album;
    }

    public string Artist => _album.Artist; // this property will not be changed in the UI

    public string Title => _album.Title; // this property will not be changed in the UI
    
    private Bitmap? _cover;

    public Bitmap? Cover
    {
        get => _cover;
        private set => this.RaiseAndSetIfChanged(ref _cover, value);
    }
    
    public async Task LoadCover()
    {
        // Load image in a background thread to avoid freeze the UI
        await using (var imageStream = await _album.LoadCoverBitmapAsync())
        {
            // Do not display the image with full resolution so only the an thumbnail 
            // of 400 width
            Cover = await Task.Run(() => Bitmap.DecodeToWidth(imageStream, 400));
        }
    }
    
    /// <summary>
    /// Save albums list to disk.
    /// </summary>
    public async Task SaveToDiskAsync()
    {
        await _album.SaveAsync();

        if (Cover != null)
        {
            var bitmap = Cover;

            await Task.Run(() =>
            {
                // Cache the album bitmap
                using (var fs = _album.SaveCoverBitmapStream())
                {
                    bitmap.Save(fs);
                }
            });
        }
    }
}