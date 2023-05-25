using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using System;
using System.Linq;
using System.Threading;
using Avalonia.MusicStore.Models;

namespace Avalonia.MusicStore.ViewModels;

public class MusicStoreViewModel: ViewModelBase
{
    private string? _searchText;
    private bool _isBusy;

    private AlbumViewModel? _selectedAlbum;
    
    /// <summary>
    /// List of albums.
    /// </summary>
    public ObservableCollection<AlbumViewModel> SearchResults { get; } = new(); // avoid to have a null list
    

    /// <summary>
    /// Command to buy an album. Unit is a dummy type.
    /// </summary>
    public ReactiveCommand<Unit, AlbumViewModel?> BuyMusicCommand { get; }
    
    public AlbumViewModel? SelectedAlbum
    {
        get => _selectedAlbum;
        set => this.RaiseAndSetIfChanged(ref _selectedAlbum, value);
    }


    public string? SearchText
    {
        get => _searchText;
        
        // Check if the value of _searchText has changed
        // so it can be notified to the View about the change
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        
        // Notify the View if the value of _isBusy property is changed
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public MusicStoreViewModel()
    {
        // Fire an event every time the user types
        this.WhenAnyValue(x => x.SearchText)
            // this event will be fired after 400 milliseconds the user stopped typing
            .Throttle(TimeSpan.FromMilliseconds(400))
            // ensure that the search operation was done in the UI thread 
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(DoSearch!);
        
        BuyMusicCommand = ReactiveCommand.Create(() =>
        {
            // Return the selected album when button is clicked
            return SelectedAlbum;
        });
    }
    
    private CancellationTokenSource? _cancellationTokenSource;

    private async void DoSearch(string s)
    {
        IsBusy = true;
        SearchResults.Clear();

        // Cancel the task that is loading covers
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        
        // _cancellationTokeSource might be replaced asynchronously
        // it has to be stored in a local variable
        var cancellationToken = _cancellationTokenSource.Token;
        
        if (!string.IsNullOrWhiteSpace(s))
        {
            var albums = await Album.SearchAsync(s);

            foreach (var album in albums)
            {
                var vm = new AlbumViewModel(album);

                SearchResults.Add(vm);
            }
            
            if (!cancellationToken.IsCancellationRequested)
            {
                LoadCovers(cancellationToken);
            }
        }

        IsBusy = false;
    }
    
    private async void LoadCovers(CancellationToken cancellationToken)
    {
        // Get a new list because SearchResults could be changed by another thread
        foreach (var album in SearchResults.ToList())
        {
            await album.LoadCover();

            // Stop loading covers when a new search is performed
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }
}