using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

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
		MediaPlayer _player = new MediaPlayer();
		DispatcherTimer _timer;
        int _lastIndex = -1;

        public MainWindow()
		{
			InitializeComponent();
            _player.MediaEnded += _player_MediaEnded;
		}

        private void _player_MediaEnded(object sender, EventArgs e)
        {
            var playList = PlayLists.SelectedItem as PlayList;
            var count = playList.ItemList.Count;
            _lastIndex = (_lastIndex + 1) % count;
            PlaySelectedIndex(_lastIndex);
        }

        public class PlayList
		{
			public string PlayListName { get; set; }
			public BindingList<FileInfo> ItemList;
		}

		BindingList<PlayList> _listPlay = new BindingList<PlayList>();
		int i = 1;
		private void newPlaylistMenuItem_Click(object sender, RoutedEventArgs e)
		{
			_listPlay.Add(new PlayList() { PlayListName = $"Play List {i}", ItemList = new BindingList<FileInfo>() });
			i++;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			PlayLists.ItemsSource = _listPlay;
			_timer = new DispatcherTimer();
			_timer.Tick += timer_Tick;
			_player.MediaOpened += _player_MediaOpened;
		}

		private void _player_MediaOpened(object sender, EventArgs e)
		{
			maxPosition.Text = _player.NaturalDuration.TimeSpan.ToString(@"mm\:ss");
			currentPosition.Text = _player.Position.ToString(@"mm\:ss");
			progressSlider.Value = 0;
			_timer.Interval = TimeSpan.FromMilliseconds(_player.NaturalDuration.TimeSpan.TotalMilliseconds / progressSlider.Maximum);
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
                screen.Filter = "music files (*.mp3;*.acc;*.flac;*.wma;*.avc;*.lossless)|*.mp3;*.acc;*.flac;*.wma;*.avc;*.lossless|All files (*.*)|*.*";
                if (screen.ShowDialog() == true)
				{
					var info = new FileInfo(screen.FileName);
					playlist.ItemList.Add(info);
				}
			}
			else
			{
				System.Windows.MessageBox.Show("No PlayList selected!");
			}
		}

		private void PlayLists_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var playList = PlayLists.SelectedItem as PlayList;
			musicListBox.ItemsSource = playList.ItemList;
		}

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			if (_player.Source != null)
			{
				if (_isPlaying == false)
                {
                    playButtonIcon.Source = _pauseBitmapImage;
                    _lastIndex = musicListBox.SelectedIndex;
                    PlaySelectedIndex(_lastIndex);
                }
                else
				{
					playButtonIcon.Source = _playBitmapImage;
					_player.Pause();
					_timer.Stop();
                    _isPlaying = false;

                }

			}
			else
			{
				MessageBox.Show("Please choose a song! ");
			}
			//_isPlaying = !_isPlaying;
		}

        private void PlaySelectedIndex(int i)
        {
            var playList = PlayLists.SelectedItem as PlayList;
            string filename = playList.ItemList[i].FullName;

            _player.Open(new Uri(filename, UriKind.Absolute));

            _player.Play();
            _isPlaying = true;
            _timer.Start();
        }

        private void MusicListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
            var song = musicListBox.SelectedItem as FileInfo;
            if (_isPlaying == true)
            {
                _timer.Stop();
                _player.Stop();
                playButtonIcon.Source = _playBitmapImage;
                _isPlaying = !_isPlaying;
            }
            _player.Open(new Uri(song.FullName, UriKind.Absolute));
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
	}
}
