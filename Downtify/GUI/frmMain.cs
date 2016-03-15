﻿using NLog;
using System;
using System.IO;
using System.Windows.Forms;

namespace Downtify.GUI
{
    public partial class frmMain : Form
    {
        SpotifyDownloader downloader;
        public static XmlConfiguration configuration;
        public static LanguageXML lang;
        private int _totalitems = 0;
        private int _currentcounter = 0;
        private bool _init = true;

        public frmMain()
        {
            InitializeComponent();

            downloader = new SpotifyDownloader();
            configuration = new XmlConfiguration("config.xml");
            downloader.OnLoginResult += OnLoginResult;
            downloader.OnDownloadComplete += downloader_OnDownloadComplete;
            downloader.OnDownloadProgress += downloader_OnDownloadProgress;
        }

        // Very ugly, todo: move parts of this to the downloader class
        private void downloader_OnDownloadComplete(bool successfully)
        {
            var list = new object[listBoxTracks.SelectedItems.Count];
            for (int i = 1; i < listBoxTracks.SelectedItems.Count; i++)
                list[i - 1] = listBoxTracks.SelectedItems[i];

            listBoxTracks.SelectedItems.Clear();

            foreach (var track in list)
                listBoxTracks.SelectedItems.Add(track);

            if (listBoxTracks.SelectedItems.Count == 0)
            {
                listBoxTracks.SelectedItems.Clear();
                MessageBox.Show(lang.GetString("download/done"));
                EnableControls(true);
                return;
            }


            var selecteditem = ((TrackItem)listBoxTracks.SelectedItems[0]);

            if (selecteditem != null)
            {
                _currentcounter += 1;

                var itemfullename = selecteditem.ToString();

                SetupStatusBar(_currentcounter, itemfullename);
                try
                {
                    downloader.LogMessage(LogLevel.Debug, "Trying getting : " + itemfullename);

                    downloader.Download(selecteditem.Track);
                }
                catch (Exception ex)
                {
                    string msg = string.Format("Failed getting item {0} /r/n cause: {1}", itemfullename, ex.Message);
                    downloader.LogMessage(LogLevel.Error, msg);
                }




            }
            
        }

        private void downloader_OnDownloadProgress(int value)
        {
            this.Invoke((MethodInvoker)delegate
            {
                if (value > 100 || value < 0)
                    return;

                 progressBar1.Value = value;
            });
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            EnableControls(false);
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {
            System.Threading.Thread.Sleep(200);
            this.Activate();

            // very ugly, use config parser (json for example) would be nicer
            string username = "", password = "";
            configuration.LoadConfigurationFile();
            TransferConfig();
            username = configuration.GetConfiguration("username");
            password = configuration.GetConfiguration("password");
            lang = new LanguageXML(configuration.GetConfiguration("language"));

            textBoxLink.Placeholder = lang.GetString("download/paste_uri");

            downloader.Login(username, password);
        }

        private void TransferConfig()
        {
            if(File.Exists("config.txt"))
            {
                string username = "", password = "";
                foreach(var currentLine in File.ReadAllLines("config.txt"))
                {
                    var line = currentLine.Trim();
                    if (line.StartsWith("#"))
                        continue;

                    if (line.StartsWith("username"))
                        username = line.Split('"')[1].Split('"')[0];
                    else if (line.StartsWith("password"))
                        password = line.Split('"')[1].Split('"')[0];
                }
                if (configuration.GetConfiguration("username") == "USERNAME")
                    configuration.SetConfigurationEntry("username", username);
                if (configuration.GetConfiguration("password") == "PASSWORD")
                    configuration.SetConfigurationEntry("password", password);
                configuration.SaveConfigurationFile();
                File.Delete("config.txt");
            }
        }

        private void OnLoginResult(bool isLoggedIn)
        {
            if (!isLoggedIn)
            {
                MessageBox.Show(lang.GetString("error/no_premium"), lang.GetString("title/error"));
                Application.Exit();
                return;
            }

            EnableControls(true);
        }

        private void EnableControls(bool enable)
        {
            foreach (var control in this.Controls)
                ((Control)control).Enabled = enable;
        }

        private async  void textBoxLink_TextChanged(object sender, EventArgs e)
        {
            var link = textBoxLink.Text;
            try
            {
                EnableControls(false);
                
                //Validate pasted URI
                if(link.Length > 0 && !link.ToLower().StartsWith("spotify:"))
                {
                    MessageBox.Show(lang.GetString("download/invalid_uri"));
                    textBoxLink.Clear();
                    return;
                }

                if (link.ToLower().Contains("playlist"))
                {
                    var playlist = await downloader.FetchPlaylist(textBoxLink.Text);
                    for (int i = 0; i < playlist.NumTracks(); i++)
                        listBoxTracks.Items.Add(new TrackItem(playlist.Track(i)));
                    textBoxLink.Clear();
                }
                else if (link.ToLower().Contains("track"))
                {
                    var track = await downloader.FetchTrack(textBoxLink.Text);
                    listBoxTracks.Items.Add(new TrackItem(track));
                    textBoxLink.Clear();
                }
                else if(link.ToLower().Contains("album"))
                {
                    var album = await downloader.FetchAlbum(textBoxLink.Text);
                    for (int i = 0; i < album.NumTracks(); i++)
                        listBoxTracks.Items.Add(new TrackItem(album.Track(i)));
                    textBoxLink.Clear();
                }
            }
            catch (NullReferenceException)
            {
            }
            finally
            {
                EnableControls(true);
            }
        }

        private void listBoxTracks_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (listBoxTracks.SelectedItems.Count == 0)
                    return;

                var list = new TrackItem[listBoxTracks.SelectedItems.Count];
                listBoxTracks.SelectedItems.CopyTo(list, 0);

                foreach (var track in list)
                    listBoxTracks.Items.Remove(track);
            }
            else if (e.KeyCode == Keys.A && e.Control)
            {
                var list = new TrackItem[listBoxTracks.Items.Count];
                listBoxTracks.Items.CopyTo(list, 0);

                listBoxTracks.SelectedItems.Clear();
                foreach (var track in list)
                    listBoxTracks.SelectedItems.Add(track);
            }
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            if (listBoxTracks.SelectedItems.Count == 0)
            {
                MessageBox.Show(lang.GetString("error/no_download_selection"), lang.GetString("title/error"));
                return;
            }

            if (_init)
            {

                _totalitems = listBoxTracks.SelectedItems.Count;
                _init = false;

            }
            EnableControls(false);
            

                    TrackItem selecteditem = ((TrackItem)listBoxTracks.SelectedItems[0]);

                    if (selecteditem != null)
                    {
                        _currentcounter += 1;
                        
                        var itemfullename = selecteditem.ToString();

                        SetupStatusBar(_currentcounter, itemfullename);
                        try
                        {
                            downloader.LogMessage(LogLevel.Debug, "Trying getting : " + itemfullename);

                            downloader.Download(selecteditem.Track);
                        }
                        catch (Exception ex)
                        {
                            string msg = string.Format("Failed getting item {0} /r/n cause: {1}", itemfullename, ex.Message);
                            downloader.LogMessage(LogLevel.Error, msg);
                        }




                    }
                }

        private void SetupStatusBar(int _currentcounter, string itemfullename)
        {
            var counterstring = string.Format("Downloading number {0} of {1}", _currentcounter.ToString(), _totalitems.ToString());
            var songdownload = string.Format("Downloading song: {0}", itemfullename);

            tsCounterText.Text = counterstring;
            tsStatusText.Text = songdownload;

        }
    }
           
        }
    

