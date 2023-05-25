using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia.MusicStore.Models;
using ReactiveUI;
using System.Reactive.Concurrency;

namespace Avalonia.MusicStore.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ICommand BuyMusicCommand { get; }
    
    public Interaction<MusicStoreViewModel, AlbumViewModel?> ShowDialog { get; }
    
    /// <summary>
    /// Tell the user that the music collection is empty.
    /// </summary>
    private bool _collectionEmpty;

    public bool CollectionEmpty
    {
        get => _collectionEmpty;
        set => this.RaiseAndSetIfChanged(ref _collectionEmpty, value);
    }

    public ObservableCollection<AlbumViewModel> Albums { get; } = new();

    public MainWindowViewModel()
    {
        ShowDialog = new Interaction<MusicStoreViewModel, AlbumViewModel?>();
        BuyMusicCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var store = new MusicStoreViewModel();

            var result = await ShowDialog.Handle(store);

            if (result != null)
            {
                Albums.Add(result);
                await result.SaveToDiskAsync();
            }
        });
        
        // Keep the CollectionEmpty property updated
        this.WhenAnyValue(x => x.Albums.Count)
            .Subscribe(x => CollectionEmpty = x == 0);

        RxApp.MainThreadScheduler.Schedule(LoadAlbums);
    }
    
    private async void LoadAlbums()
    {
        var albums = (await Album.LoadCachedAsync()).Select(x => new AlbumViewModel(x));

        foreach (var album in albums)
        {
            Albums.Add(album);
        }

        foreach (var album in Albums.ToList())
        {
            await album.LoadCover();
        }
    }
}