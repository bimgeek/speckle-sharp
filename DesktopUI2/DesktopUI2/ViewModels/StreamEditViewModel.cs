using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Metadata;
using DesktopUI2.Models;
using DesktopUI2.Models.Filters;
using DesktopUI2.Views;
using Material.Dialog;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Splat;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels
{
  public class StreamEditViewModel : StreamViewModelBase, IRoutableViewModel
  {
    public IScreen HostScreen { get; }

    public string UrlPathSegment { get; } = "stream";

    private Client Client { get; }

    #region bindings

    private ConnectorBindings Bindings;

    public ReactiveCommand<Unit, Unit> GoBack => MainWindowViewModel.RouterInstance.NavigateBack;

    private bool _isReceiver = false;
    public bool IsReceiver
    {
      get => _isReceiver;
      set
      {
        this.RaiseAndSetIfChanged(ref _isReceiver, value);
      }
    }

    private Branch _selectedBranch;
    public Branch SelectedBranch
    {
      get => _selectedBranch;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedBranch, value);

        if (value != null)
          GetCommits();

      }
    }

    private List<Branch> _branches;
    public List<Branch> Branches
    {
      get => _branches;
      private set => this.RaiseAndSetIfChanged(ref _branches, value);
    }


    private Commit _selectedCommit;
    public Commit SelectedCommit
    {
      get => _selectedCommit;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedCommit, value);
      }
    }

    private List<Commit> _commits;
    public List<Commit> Commits
    {
      get => _commits;
      private set
      {
        this.RaiseAndSetIfChanged(ref _commits, value);
        this.RaisePropertyChanged("HasCommits");
      }
    }


    private FilterViewModel _selectedFilter;
    public FilterViewModel SelectedFilter
    {
      get => _selectedFilter;
      set
      {
        //trigger change when any property in the child model view changes
        //used for the CanSave etc button bindings
        value.PropertyChanged += (s, eo) =>
        {
          this.RaisePropertyChanged("SelectedFilter");
        };
        this.RaiseAndSetIfChanged(ref _selectedFilter, value);
      }
    }


    private List<FilterViewModel> _filters;
    public List<FilterViewModel> Filters
    {
      get => _filters;
      private set => this.RaiseAndSetIfChanged(ref _filters, value);
    }

    public bool HasCommits => Commits != null && Commits.Any();

    #endregion

    private StreamState _streamState { get; }


    public StreamEditViewModel()
    {
    }

    public StreamEditViewModel(IScreen screen, StreamState streamState)
    {
      HostScreen = screen;
      Stream = streamState.CachedStream;
      Client = streamState.Client;
      _streamState = streamState; //cached, should not be accessed


      //use dependency injection to get bindings
      Bindings = Locator.Current.GetService<ConnectorBindings>();

      //get available filters from our bindings
      Filters = new List<FilterViewModel>(Bindings.GetSelectionFilters().Select(x => new FilterViewModel(x)));
      SelectedFilter = Filters[0];

      GetBranchesAndRestoreState(streamState.Client, streamState);
    }


    private async void GetBranchesAndRestoreState(Client client, StreamState streamState)
    {
      //this is for design mode only
      if (Design.IsDesignMode && Stream == null)
      {
        Stream = (await client.StreamsGet())[0];
      }
      var branches = await client.StreamGetBranches(Stream.id, 100, 0);
      branches.Reverse();
      Branches = branches;

      //restore state
      IsReceiver = streamState.IsReceiver;

      var branch = Branches.FirstOrDefault(x => x.name == streamState.BranchName);
      if (branch != null)
        SelectedBranch = branch;
      else
        SelectedBranch = Branches[0];

      if (streamState.Filter != null)
      {
        SelectedFilter = Filters.FirstOrDefault(x => x.Filter.Slug == streamState.Filter.Slug);
        if (SelectedFilter != null)
          SelectedFilter.Filter = streamState.Filter;
      }
    }

    /// <summary>
    /// The model Stream state, generate it on the fly when needed
    /// </summary>
    private StreamState GetStreamState()
    {
      _streamState.BranchName = SelectedBranch.name;
      _streamState.IsReceiver = IsReceiver;
      if (IsReceiver)
        _streamState.CommitId = SelectedCommit.id;
      if (!IsReceiver)
        _streamState.Filter = SelectedFilter.Filter;
      return _streamState;


    }

    private async void GetCommits()
    {
      if (SelectedBranch.commits == null || SelectedBranch.commits.totalCount > 0)
      {
        var branch = await Client.BranchGet(Stream.id, SelectedBranch.name, 100);
        branch.commits.items.Insert(0, new Commit { id = "latest", message = "Always receive the latest commit sent to this branch." });
        Commits = branch.commits.items;
        SelectedCommit = Commits[0];
      }
      else
      {
        SelectedCommit = null;
        Commits = new List<Commit>();
        SelectedCommit = null;
      }
      //else
      //{
      //  Commits = new List<Commit>() { new Commit { id = "latest", message = "This branch has no commits." } };
      //}


    }

    private void SaveCommand()
    {
      MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
      HomeViewModel.Instance.AddSavedStream(GetStreamState());
    }

    private async void SendCommand()
    {
      Progress = new ProgressViewModel();
      var dialog = Utils.SendReceiveDialog("Sending...", this);


      _ = dialog.ShowDialog(MainWindow.Instance).ContinueWith(x =>
      {
        if (x.Result.GetResult == "cancel")
          Progress.CancellationTokenSource.Cancel();
      }
        );

      await Bindings.SendStream(GetStreamState(), Progress);
      dialog.GetWindow().Close();
      //TODO: display other dialog if operation failed etc
      MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
    }

    private async void ReceiveCommand()
    {
      Progress = new ProgressViewModel();
      var dialog = Utils.SendReceiveDialog("Receiving...", this);


      _ = dialog.ShowDialog(MainWindow.Instance).ContinueWith(x =>
      {
        if (x.Result.GetResult == "cancel")
          Progress.CancellationTokenSource.Cancel();
      }
        );

      await Bindings.ReceiveStream(GetStreamState(), Progress);
      dialog.GetWindow().Close();
      //TODO: display other dialog if operation failed etc
      MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
    }

    private void SaveSendCommand()
    {
      MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
      HomeViewModel.Instance.AddSavedStream(GetStreamState(), true);
    }

    private void SaveReceiveCommand()
    {
      MainWindowViewModel.RouterInstance.Navigate.Execute(HomeViewModel.Instance);
      HomeViewModel.Instance.AddSavedStream(GetStreamState(), false, true);
    }

    [DependsOn(nameof(SelectedBranch))]
    [DependsOn(nameof(SelectedFilter))]
    [DependsOn(nameof(SelectedCommit))]
    [DependsOn(nameof(IsReceiver))]
    private bool CanSaveCommand(object parameter)
    {
      return IsReady();
    }

    [DependsOn(nameof(SelectedBranch))]
    [DependsOn(nameof(SelectedFilter))]
    [DependsOn(nameof(IsReceiver))]
    private bool CanSaveSendCommand(object parameter)
    {
      return IsReady();
    }

    [DependsOn(nameof(SelectedBranch))]
    [DependsOn(nameof(SelectedCommit))]
    [DependsOn(nameof(IsReceiver))]
    private bool CanSaveReceiveCommand(object parameter)
    {
      return IsReady();
    }

    [DependsOn(nameof(SelectedBranch))]
    [DependsOn(nameof(SelectedFilter))]
    [DependsOn(nameof(IsReceiver))]
    private bool CanSendCommand(object parameter)
    {
      return IsReady();
    }

    [DependsOn(nameof(SelectedBranch))]
    [DependsOn(nameof(SelectedCommit))]
    [DependsOn(nameof(IsReceiver))]
    private bool CanReceiveCommand(object parameter)
    {
      return IsReady();
    }

    private bool IsReady()
    {
      if (SelectedBranch == null)
        return false;

      if (!IsReceiver)
      {
        if (SelectedFilter == null)
          return false;
        if (!SelectedFilter.IsReady())
          return false;
      }
      else
      {
        if (SelectedCommit == null)
          return false;
      }

      return true;
    }


  }
}