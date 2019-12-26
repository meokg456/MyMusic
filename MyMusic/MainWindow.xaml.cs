using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using ListBox = System.Windows.Controls.ListBox;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.MessageBox;
using SelectionMode = System.Windows.Controls.SelectionMode;
using TextBox = System.Windows.Controls.TextBox;

namespace MyMusic
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		bool _isPlaying = false;
		BitmapImage _pauseBitmapImage = new BitmapImage(new Uri("Icons/pause.png", UriKind.Relative));
		BitmapImage _playBitmapImage = new BitmapImage(new Uri("Icons/play button.png", UriKind.Relative));
		BitmapImage _muteBitmapImage = new BitmapImage(new Uri("Icons/mute.png", UriKind.Relative));
		BitmapImage _volumeBitmapImage = new BitmapImage(new Uri("Icons/volume.png", UriKind.Relative));
		MediaPlayer _player = new MediaPlayer();
		DispatcherTimer _timer;
		RepeatOption _repeatOption = RepeatOption.NoRepeat;
		NextOption _nextOption = NextOption.SequenceNext;
		IKeyboardMouseEvents _hook;
		Stack<FileInfo> _playedSongs = new Stack<FileInfo>();
		Stack<FileInfo> _nextSongs = new Stack<FileInfo>();
		string FileNamePlayList = "PlayList.txt";
		MenuItem _renameMenuItem;
		string _renamingFileName;

		public MainWindow()
		{
			InitializeComponent();
		}

		public enum RepeatOption
		{
			NoRepeat = -1,
			InfinityRepeat = 0,
			OneTimeRepeat = 1
		}

		public enum NextOption
		{
			RandomNext = 0,
			SequenceNext = 1
		}

		public class PlayList : INotifyPropertyChanged
		{
			private string _playlistName;
			public string PlayListName
			{
				get
				{ return _playlistName; }
				set
				{
					_playlistName = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PlayListName"));
				}
			}
			public BindingList<FileInfo> ItemList;

			public event PropertyChangedEventHandler PropertyChanged;
		}

		BindingList<PlayList> _playlists = new BindingList<PlayList>();
		int countPlayList = 1;
		private void newPlaylistMenuItem_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                while (File.Exists($"Play List {countPlayList}.txt"))
                {
                    countPlayList++;
                }
                _playlists.Add(new PlayList() { PlayListName = $"Play List {countPlayList}", ItemList = new BindingList<FileInfo>() });

                File.Create($"Play List {countPlayList}.txt").Close();
                countPlayList++;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
            try
            {
                playlistListBox.ItemsSource = _playlists;
                musicListBox.SelectionMode = SelectionMode.Extended;
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(250);
                _timer.Tick += timer_Tick;
                _player.MediaOpened += _player_MediaOpened;
                _player.MediaEnded += _player_MediaEnded;
                volumeSlider.Value = 75;
                _player.Volume = 0.75;
                _hook = Hook.GlobalEvents();
                _hook.KeyUp += Hook_KeyUp;
                LoadPlayList();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void Hook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
            try
            {
                if (e.Control && e.Alt && e.KeyCode == Keys.NumPad0)
                {
                    PlayButton_Click(sender, null);
                }
                if (e.Control && e.Alt && e.KeyCode == Keys.Right)
                {
                    NextButton_Click(sender, null);
                }
                if (e.Control && e.Alt && e.KeyCode == Keys.Left)
                {
                    PreviousButton_Click(sender, null);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void _player_MediaEnded(object sender, EventArgs e)
		{
            try
            {
                var playlist = playlistListBox.SelectedItem as PlayList;
                if (playlistListBox.SelectedIndex < 0) return;
                var count = playlist.ItemList.Count;
                var index = musicListBox.SelectedIndex;
                StopButton_Click(sender, e as RoutedEventArgs);
                _playedSongs.Push(musicListBox.SelectedItem as FileInfo);
                if (_repeatOption == RepeatOption.NoRepeat && _nextOption == NextOption.SequenceNext)
                {
                    index = index + 1;
                    if (index == count)
                    {
                        progressSlider.Value = 0;
                        return;
                    }
                }
                if (_repeatOption == RepeatOption.InfinityRepeat)
                {
                    progressSlider.Value = 0;
                    PlayButton_Click(sender, e as RoutedEventArgs);
                    return;
                }
                if (_repeatOption == RepeatOption.OneTimeRepeat && _nextOption == NextOption.SequenceNext)
                {
                    index = (index + 1) % count;
                }
                if (_nextOption == NextOption.RandomNext)
                {
                    var oldIndex = index;

                    var rand = new Random();
                    do
                    {
                        index = rand.Next(count);
                    } while (index == oldIndex && count > 1);
                }
                musicListBox.SelectedItem = playlist.ItemList[index];
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void _player_MediaOpened(object sender, EventArgs e)
		{
            try
            {
                maxPosition.Text = _player.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
                currentPosition.Text = _player.Position.ToString(@"mm\:ss");
                progressSlider.Value = 0;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void timer_Tick(object sender, EventArgs e)
		{
            try
            {
                progressSlider.Value = _player.Position.TotalMilliseconds / _player.NaturalDuration.TimeSpan.TotalMilliseconds * progressSlider.Maximum;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void addSongMenuItem_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                if (playlistListBox.SelectedIndex >= 0)
                {
                    var playlist = playlistListBox.SelectedItem as PlayList;
                    var screen = new Microsoft.Win32.OpenFileDialog();
                    screen.Multiselect = true;
                    screen.Filter = "music files (*.mp3;*.acc;*.flac;*.wma;*.avc;*.lossless)|*.mp3;*.acc;*.flac;*.wma;*.avc;*.lossless|All files (*.*)|*.*";
                    if (screen.ShowDialog() == true)
                    {
                        foreach (var fileName in screen.FileNames)
                        {
                            var info = new FileInfo(fileName);
                            playlist.ItemList.Add(info);

                        }
                        musicListBox.ScrollIntoView(playlist.ItemList[playlist.ItemList.Count - 1]);
                        musicListBox.ScrollIntoView(playlist.ItemList[0]);
                    }

                }
                else
                {
                    System.Windows.MessageBox.Show("No PlayList selected!");
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void playlistListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
            try
            {
                var playlist = playlistListBox.SelectedItem as PlayList;
                if (_isPlaying == true)
                {
                    StopButton_Click(sender, null);
                    _playedSongs.Clear();
                    _nextSongs.Clear();
                }
                if (playlist == null)
                {
                    musicListBox.ItemsSource = null;
                    return;
                }
                musicListBox.ItemsSource = playlist.ItemList;
                if (playlist.ItemList.Count > 0)
                {
                    musicListBox.ScrollIntoView(playlist.ItemList[playlist.ItemList.Count - 1]);
                    musicListBox.ScrollIntoView(playlist.ItemList[0]);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                if (_player.Source != null)
                {
                    if (_isPlaying == false)
                    {
                        playButtonIcon.Source = _pauseBitmapImage;
                        _player.Play();
                        _timer.Start();
                    }
                    else
                    {
                        playButtonIcon.Source = _playBitmapImage;
                        _player.Pause();
                        _timer.Stop();
                    }
                }
                else
                {
                    MessageBox.Show("Please choose a song!");
                }
                _isPlaying = !_isPlaying;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void MusicListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
            try
            {
                if (musicListBox.SelectedItems.Count > 1) return;
                var song = musicListBox.SelectedItem as FileInfo;
                if (song == null) return;
                StopButton_Click(sender, e);
                _player.Open(new Uri(song.FullName, UriKind.Absolute));
                while (_player.NaturalDuration.HasTimeSpan == false) ;
                musicListBox.ScrollIntoView(song);
                PlayButton_Click(sender, e);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
            try
            {
                if (_player.Source != null)
                {
                    _player.Position = TimeSpan.FromMilliseconds(progressSlider.Value / progressSlider.Maximum * _player.NaturalDuration.TimeSpan.TotalMilliseconds);
                    currentPosition.Text = _player.Position.ToString(@"mm\:ss");
                }
                else
                {
                    progressSlider.Value = 0;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void SelfRepeat_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                var button = sender as Button;
                _repeatOption = RepeatOption.NoRepeat;
                infinityRepeatButton.Tag = "";
                infinityRepeatButton.Visibility = Visibility.Visible;
                button.Visibility = Visibility.Hidden;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

        private void SequenRepeat_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button.Tag as string != "chose")
                {
                    _repeatOption = RepeatOption.OneTimeRepeat;
                    button.Tag = "chose";
                }
                else
                {
                    _repeatOption = RepeatOption.InfinityRepeat;
                    button.Tag = "";
                    selfRepeatButton.Visibility = Visibility.Visible;
                    button.Visibility = Visibility.Hidden;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

        private void RandomNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button.Tag as string != "chose")
                {
                    _nextOption = NextOption.RandomNext;
                    button.Tag = "chose";
                }
                else
                {
                    _nextOption = NextOption.SequenceNext;
                    button.Tag = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void MuteButton_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                _player.IsMuted = !_player.IsMuted;
                if (_player.IsMuted == true)
                {
                    muteButtonIcon.Source = _muteBitmapImage;
                }
                else
                {
                    muteButtonIcon.Source = _volumeBitmapImage;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void MuteSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
            try
            {
                _player.IsMuted = !_player.IsMuted;
                if (_player.IsMuted == true)
                {
                    muteButtonIcon.Source = _muteBitmapImage;
                }
                else
                {
                    muteButtonIcon.Source = _volumeBitmapImage;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void PreviousButton_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                if (_playedSongs.Count > 0)
                {
                    _nextSongs.Push(musicListBox.SelectedItem as FileInfo);
                    StopButton_Click(sender, e);
                    musicListBox.SelectedItem = _playedSongs.Pop();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void NextButton_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                var playlist = playlistListBox.SelectedItem as PlayList;
                if (playlistListBox.SelectedIndex < 0) return;
                StopButton_Click(sender, e);
                if (_nextSongs.Count == 0)
                {
                    _player_MediaEnded(sender, e);
                }
                else
                {
                    _playedSongs.Push(musicListBox.SelectedItem as FileInfo);
                    musicListBox.SelectedItem = _nextSongs.Pop();
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

        private void SavePlayList()
        {
            try
            {
                var writePlayList = new StreamWriter(FileNamePlayList);
                //dong dau tien save so luong PlayList
                writePlayList.WriteLine(_playlists.Count);
                //dong thu hai save progressSlider
                writePlayList.WriteLine(playlistListBox.SelectedIndex);


                if (_playlists.Count > 0)
                {
                    //tiep theo la ghi ten cac tap tin chua bai hat cua moi playlist
                    foreach (var item in _playlists)
                    {
                        var namePlayList = item.PlayListName + ".txt";
                        writePlayList.WriteLine(namePlayList);
                        var playlist = playlistListBox.SelectedItem as PlayList;

                        var writeItemList = new StreamWriter(namePlayList);
                        //dong dau tien save so bai hat
                        writeItemList.WriteLine(item.ItemList.Count);
                        writeItemList.WriteLine(musicListBox.SelectedIndex);

                        if (item.ItemList.Count > 0)
                        {
                            foreach (var song in item.ItemList)
                            {
                                var urlSong = song.Directory + "\\" + song.Name;
                                writeItemList.WriteLine(urlSong);
                                var itemlist = musicListBox.SelectedItem as FileInfo;
                            }
                        }
                        writeItemList.Close();
                    }
                }
                writePlayList.Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadPlayList()
        {
			try
			{
				var readerPlayList = new StreamReader(FileNamePlayList);
				var firstLine = readerPlayList.ReadLine();
				var numPlayList = int.Parse(firstLine);
				var secondLine = readerPlayList.ReadLine();
				var playingPlaylistIndex = int.Parse(secondLine);
				int playingSongIndex = -1;
				if (numPlayList > 0)
				{
					for (int i = 0; i < numPlayList; i++)
					{
						var namePlayList = readerPlayList.ReadLine();
						var playlist = namePlayList.Replace(".txt", "");

                        var readerItemList = new StreamReader(namePlayList);
						var numItemList = int.Parse(readerItemList.ReadLine());
						playingSongIndex =  int.Parse(readerItemList.ReadLine());
						var itemListPlay = new PlayList() { PlayListName = playlist, ItemList = new BindingList<FileInfo>() };

						if (numItemList > 0)
						{
							for (int j = 0; j < numItemList; j++)
							{
								var urlSong = readerItemList.ReadLine();
                                var song = new FileInfo(urlSong);
                                itemListPlay.ItemList.Add(song);

                            }
						}
                        
                        _playlists.Add(itemListPlay);
                        readerItemList.Close();
					}
					if (playingPlaylistIndex >= 0)
					{
						playlistListBox.SelectedItem = _playlists[playingPlaylistIndex];
					}
					if(playingSongIndex >= 0)
					{
						musicListBox.SelectedItem = _playlists[playingPlaylistIndex].ItemList[playingSongIndex];

					}
					countPlayList = numPlayList + 1;
				}
				readerPlayList.Close();

            }
			catch(Exception ex)
			{
                MessageBox.Show(ex.Message);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                _hook.KeyUp -= Hook_KeyUp;
                _hook.Dispose();
                SavePlayList();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (_renameMenuItem == null)
                {
                    if (e.Key == Key.Space)
                    {
                        PlayButton_Click(sender, null);
                    }
                    if (e.Key == Key.Right)
                    {
                        progressSlider.Value = progressSlider.Value + 1200;
                    }
                    if (e.Key == Key.Left)
                    {
                        progressSlider.Value = progressSlider.Value - 1200;
                    }
                    if (e.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl) || e.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        musicListBox.SelectAll();
                    }


                }
                else
            if (e.Key == Key.Enter)
                {
                    if ((string)_renameMenuItem.Tag == "Clicked")
                    {
                        var index = playlistListBox.SelectedIndex;
                        try
                        {

                            _renameMenuItem.Tag = "";
                            File.Move(_renamingFileName + ".txt", _playlists[index].PlayListName + ".txt");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                            _playlists[index].PlayListName = _renamingFileName;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void PlaylistRenameMenuItem_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                var playlist = playlistListBox.SelectedItem as PlayList;
                _renameMenuItem = sender as MenuItem;
                _renameMenuItem.Tag = "Clicked";
                _renamingFileName = playlist.PlayListName;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void PlaylistDeleteMenuItem_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                var playlist = playlistListBox.SelectedItem as PlayList;
                playlist.ItemList.Clear();
                _playlists.Remove(playlist);
                playlistListBox.SelectedItem = null;
                File.Delete(playlist.PlayListName + ".txt");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void PlaylistName_GotFocus(object sender, RoutedEventArgs e)
		{
            try
            {
                var tb = sender as TextBox;
                tb.SelectAll();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void MusicDeleteMenuItem_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                var playlist = playlistListBox.SelectedItem as PlayList;
                var songs = musicListBox.SelectedItems;
                while (songs.Count != 0)
                {
                    playlist.ItemList.Remove(songs[0] as FileInfo);
                }
                StopButton_Click(sender, e);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}

		private void StopButton_Click(object sender, RoutedEventArgs e)
		{
            try
            {
                if (_player.Source != null)
                {
                    if (_isPlaying == true)
                    {
                        progressSlider.Value = 0;
                        _timer.Stop();
                        _player.Stop();
                        playButtonIcon.Source = _playBitmapImage;
                        _isPlaying = !_isPlaying;
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
		}
	}
}
