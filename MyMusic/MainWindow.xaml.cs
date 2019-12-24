using Gma.System.MouseKeyHook;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;

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
        RepeatOption repeatOption = RepeatOption.NoRepeat;
		IKeyboardMouseEvents _hook;
		public MainWindow()
		{
			InitializeComponent();
		}
		public enum RepeatOption
		{
			NoRepeat = -1,
			SelfRepeat = 0,
			SequenceRepeat = 1,
			RandomRepeat = 2,
		}

        
        public class PlayList
		{
			public string PlayListName { get; set; }
			public BindingList<FileInfo> ItemList;
			
		}

		BindingList<PlayList> _listPlay = new BindingList<PlayList>();
		int countPlayList = 1;
		private void newPlaylistMenuItem_Click(object sender, RoutedEventArgs e)
		{
			_listPlay.Add(new PlayList() { PlayListName = $"Play List {countPlayList}", ItemList = new BindingList<FileInfo>() });
            countPlayList++;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
            //LoadPlayList();
			PlayLists.ItemsSource = _listPlay;
			_timer = new DispatcherTimer();
			_timer.Interval = TimeSpan.FromMilliseconds(250);
			_timer.Tick += timer_Tick;
			_player.MediaOpened += _player_MediaOpened;
			_player.MediaEnded += _player_MediaEnded;
			volumeSlider.Value = 75;
			_player.Volume = 0.75;
			_hook = Hook.GlobalEvents();
			_hook.KeyUp += Hook_KeyUp;
		}

		private void Hook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
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

		private void _player_MediaEnded(object sender, EventArgs e)
		{
			_playedSongs.Push(musicListBox.SelectedItem as FileInfo);
			var index = musicListBox.SelectedIndex;
            var playlist = PlayLists.SelectedItem as PlayList;
			if (PlayLists.SelectedIndex < 0) return;
			var count = playlist.ItemList.Count;
			if (repeatOption == RepeatOption.NoRepeat)
			{
				PlayButton_Click(sender, e as RoutedEventArgs);
				progressSlider.Value = 0;
				ProgressSlider_ValueChanged(sender, e as RoutedPropertyChangedEventArgs<double>);
				return;
			}
            if (repeatOption == RepeatOption.SelfRepeat)
            {
            }
            else if (repeatOption == RepeatOption.SequenceRepeat)
            {
                index = (index + 1) % count;
            }
            else if (repeatOption == RepeatOption.RandomRepeat)
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

		private void _player_MediaOpened(object sender, EventArgs e)
		{
			maxPosition.Text = _player.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
			currentPosition.Text = _player.Position.ToString(@"mm\:ss");
			progressSlider.Value = 0;
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			currentPosition.Text = _player.Position.ToString(@"mm\:ss");
			progressSlider.Value = _player.Position.TotalMilliseconds / _player.NaturalDuration.TimeSpan.TotalMilliseconds * progressSlider.Maximum;
		}

		private void addSongMenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (PlayLists.SelectedIndex >= 0)
			{
				var playlist = PlayLists.SelectedItem as PlayList;
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
				}
			
			}
			else
			{
				System.Windows.MessageBox.Show("No PlayList selected!");
			}
		}

		private void playlistListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var playList = PlayLists.SelectedItem as PlayList;
			musicListBox.ItemsSource = playList.ItemList;
			if (_isPlaying == true)
			{
				playButtonIcon.Source = _playBitmapImage;
				_player.Stop();
				_timer.Stop();
				_player.Close();
				_isPlaying = !_isPlaying;
				_playedSongs.Clear();
				_nextSongs.Clear();
			}
		}
		private void PlayButton_Click(object sender, RoutedEventArgs e)
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
				MessageBox.Show("Please choose a song! ");
			}
			_isPlaying = !_isPlaying;
		}

        private void MusicListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var song = musicListBox.SelectedItem as FileInfo;
			if (song == null) return;
			if (_isPlaying == true)
			{
				_timer.Stop();
				_player.Stop();
				_player.Close();
				playButtonIcon.Source = _playBitmapImage;
				_isPlaying = !_isPlaying;
			}
			_player.Open(new Uri(song.FullName, UriKind.Absolute));
			while (_player.NaturalDuration.HasTimeSpan == false);
			musicListBox.ScrollIntoView(song);
			PlayButton_Click(sender, e);
		}

		private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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

        private void SelfRepeat_Click(object sender, RoutedEventArgs e)
        {
			var button = sender as Button;
			if(button.Tag as string != "chose")
			{
				repeatOption = RepeatOption.SelfRepeat;
				button.Tag = "chose";
				sequenRepeat.Tag = "";
				randomRepeat.Tag = "";
			}
			else
			{
				repeatOption = RepeatOption.NoRepeat;
				button.Tag = "";
			}
        }

        private void SequenRepeat_Click(object sender, RoutedEventArgs e)
        {
			var button = sender as Button;
			if (button.Tag as string != "chose")
			{
				repeatOption = RepeatOption.SequenceRepeat;
				button.Tag = "chose";
				selfRepeat.Tag = "";
				randomRepeat.Tag = "";
			}
			else
			{
				repeatOption = RepeatOption.NoRepeat;
				button.Tag = "";
			}
		}

        private void RandomRepeat_Click(object sender, RoutedEventArgs e)
        {
			var button = sender as Button;
			if (button.Tag as string != "chose")
			{
				repeatOption = RepeatOption.RandomRepeat;
				button.Tag = "chose";
				sequenRepeat.Tag = "";
				selfRepeat.Tag = "";
			}
			else
			{
				repeatOption = RepeatOption.NoRepeat;
				button.Tag = "";
			}
		}
		private void MuteButton_Click(object sender, RoutedEventArgs e)
		{
			_player.IsMuted = !_player.IsMuted;
			if(_player.IsMuted == true)
			{
				muteButtonIcon.Source = _muteBitmapImage;
			}
			else
			{
				muteButtonIcon.Source = _volumeBitmapImage;
			}
			
		}

		private void MuteSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_player.Volume = volumeSlider.Value / volumeSlider.Maximum;
		}
		Stack<FileInfo> _playedSongs = new Stack<FileInfo>();
		Stack<FileInfo> _nextSongs = new Stack<FileInfo>();
		private void PreviousButton_Click(object sender, RoutedEventArgs e)
		{
			if(_playedSongs.Count > 0)
			{
				_nextSongs.Push(musicListBox.SelectedItem as FileInfo);
				musicListBox.SelectedItem = _playedSongs.Pop();

			}
		}

		private void NextButton_Click(object sender, RoutedEventArgs e)
		{
			var index = musicListBox.SelectedIndex;
			var playlist = PlayLists.SelectedItem as PlayList;
			if (PlayLists.SelectedIndex < 0) return;
			var count = playlist.ItemList.Count;
			if (repeatOption == RepeatOption.NoRepeat)
			{
				_playedSongs.Push(musicListBox.SelectedItem as FileInfo);
				index = (index + 1) % count;
				musicListBox.SelectedItem = playlist.ItemList[index];
			}
			else
			{
				if (_nextSongs.Count == 0)
				{
					_player_MediaEnded(sender, e);
				}
				else
				{
					_playedSongs.Push(musicListBox.SelectedItem as FileInfo);
					musicListBox.SelectedItem = _nextSongs.Pop();
					MusicListBox_SelectionChanged(sender, e as SelectionChangedEventArgs);
					
				}
			}
		}

        string FileNamePlayList = "PlayList.txt";
        private void saveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var writePlayList = new StreamWriter(FileNamePlayList);
            //dong dau tien save so luong PlayList
            writePlayList.WriteLine(_listPlay.Count);
            if (_listPlay.Count > 0)
            {
                //tiep theo la ghi ten cac tap tin chua bai hat cua moi playlist
                foreach (var item in _listPlay)
                {
                    var namePlayList = item.PlayListName + ".txt";
                    writePlayList.WriteLine(namePlayList);

                    var writeItemList = new StreamWriter(namePlayList);
                    //dong dau tien save so bai hat
                    writeItemList.WriteLine(item.ItemList.Count);
                    if (item.ItemList.Count > 0)
                    {
                        foreach (var song in item.ItemList)
                        {
                            var urlSong = song.Directory + "\\" + song.Name;
                            writeItemList.WriteLine(urlSong);
                        }
                    }
                    writeItemList.Close();
                }
            }
            writePlayList.Close();
        }

        private void SavePlayList()
        {
            var writePlayList = new StreamWriter(FileNamePlayList);
            //dong dau tien save so luong PlayList
            writePlayList.WriteLine(_listPlay.Count);
            if (_listPlay.Count > 0)
            {
                //tiep theo la ghi ten cac tap tin chua bai hat cua moi playlist
                foreach (var item in _listPlay)
                {
                    var namePlayList = item.PlayListName + ".txt";
                    writePlayList.WriteLine(namePlayList);

                    var writeItemList = new StreamWriter(namePlayList);
                    //dong dau tien save so bai hat
                    writeItemList.WriteLine(item.ItemList.Count);
                    if (item.ItemList.Count > 0)
                    {
                        foreach (var song in item.ItemList)
                        {
                            var urlSong = song.Directory + "\\" + song.Name;
                            writeItemList.WriteLine(urlSong);
                        }
                    }
                    writeItemList.Close();
                }
            }
            writePlayList.Close();
        }

        private void loadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var readerPlayList = new StreamReader(FileNamePlayList);
            var firstLine = readerPlayList.ReadLine();
            var numPlayList = int.Parse(firstLine);

            if (numPlayList > 0)
            {
                for(int i = 0; i < numPlayList; i++)
                {
                    var namePlayList = readerPlayList.ReadLine();
                    var playlist = namePlayList.Replace(".txt", "");
                    var readerItemList = new StreamReader(namePlayList);
                    var numItemList = int.Parse(readerItemList.ReadLine());
                    var iTemListPlay = new PlayList() { PlayListName = playlist, ItemList = new BindingList<FileInfo>() };

                    if (numItemList > 0)
                    {
                        for(int j = 0; j < numItemList; j++)
                        {
                            var urlSong = readerItemList.ReadLine();
                            iTemListPlay.ItemList.Add(new FileInfo(urlSong));
                        }
                    }
                    _listPlay.Add(iTemListPlay);
                }
            }
        }

        private void LoadPlayList()
        {
            var readerPlayList = new StreamReader(FileNamePlayList);
            var firstLine = readerPlayList.ReadLine();
            var numPlayList = int.Parse(firstLine);

            if (numPlayList > 0)
            {
                for (int i = 0; i < numPlayList; i++)
                {
                    var namePlayList = readerPlayList.ReadLine();
                    var playlist = namePlayList.Replace(".txt", "");
                    var readerItemList = new StreamReader(namePlayList);
                    var numItemList = int.Parse(readerItemList.ReadLine());
                    var iTemListPlay = new PlayList() { PlayListName = playlist, ItemList = new BindingList<FileInfo>() };

                    if (numItemList > 0)
                    {
                        for (int j = 0; j < numItemList; j++)
                        {
                            var urlSong = readerItemList.ReadLine();
                            iTemListPlay.ItemList.Add(new FileInfo(urlSong));
                        }
                    }
                    _listPlay.Add(iTemListPlay);
                }
                countPlayList = numPlayList + 1;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SavePlayList();
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
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
        }

        private void PlayLists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var playList = PlayLists.SelectedItem as PlayList;
            musicListBox.ItemsSource = playList.ItemList;
            if (_isPlaying == true)
            {
                playButtonIcon.Source = _playBitmapImage;
                _player.Stop();
                _timer.Stop();
                _player.Close();
                _isPlaying = !_isPlaying;
                _playedSongs.Clear();
                _nextSongs.Clear();
            }
        }
    }
}
