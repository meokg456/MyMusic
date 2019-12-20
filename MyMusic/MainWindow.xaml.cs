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

namespace MyMusic
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}
        public class PlayList
        {
            public string NamePlayList { get; set; }
            public BindingList<FileInfo> ItemList;
        }

        BindingList<PlayList> _listPlay = new BindingList<PlayList>();
        int i = 1;
        private void newPlaylistMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _listPlay.Add(new PlayList() { NamePlayList = $"Play List {i}", ItemList = new BindingList<FileInfo>() });
            i++;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PlayLists.ItemsSource = _listPlay;
        }

        private void addSongMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (PlayLists.SelectedIndex >= 0)
            {
                var playlist = PlayLists.SelectedItem as PlayList;
                var screen = new Microsoft.Win32.OpenFileDialog();
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
    }
}
