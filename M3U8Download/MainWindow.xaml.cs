using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace M3U8Download
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Model _data = new Model();
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = _data;
        }

        private async void btn_Download(object sender, RoutedEventArgs e)
        {
            try
            {
                btnDownload.IsEnabled = false;
                var folder = System.IO.Path.GetDirectoryName(_data.SavePath);
                if (System.IO.Directory.Exists(folder) == false)
                {
                    System.IO.Directory.CreateDirectory(folder);
                }
                var url = new Uri(_data.Url);
                System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                client.Timeout = new TimeSpan(0, 0, 8);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/11.0 Mobile/15A372 Safari/604.1");
                var m3u8content = await (await client.GetAsync(_data.Url)).Content.ReadAsStringAsync();
                var lines = m3u8content.Split('\n');
                lines = lines.Where(m => m.Trim().EndsWith(".ts")).Select(m=>m.Trim()).ToArray();

                var serverUrl = $"{url.Scheme}://{url.Host}{(url.IsDefaultPort ? "" : ":" + url.Port)}";
                //List<string> filenames = new List<string>();
                StringBuilder m3u8filelist = new StringBuilder();
                var tmpFolder = $"{AppDomain.CurrentDomain.BaseDirectory}temp";
               
                if (System.IO.Directory.Exists(tmpFolder) == false)
                {
                    System.IO.Directory.CreateDirectory(tmpFolder);
                }
                for (int i = 0; i < lines.Length; i ++)
                {
                    var line = lines[i];

                    while(true)
                    {
                        try
                        {
                            var fileurl = $"{serverUrl}{line}";
                            var filedata = await (await client.GetAsync(fileurl)).Content.ReadAsByteArrayAsync();
                            var filename = $"{tmpFolder}\\{ System.IO.Path.GetFileName(line)}";
                            System.IO.File.WriteAllBytes(filename, filedata);
                            // filenames.Add(filename);
                            m3u8filelist.AppendLine($"file '{filename}'");
                            this.Title =( i * 100.0 / lines.Length).ToString("f2") + "%";
                            _data.Error = null;
                            break;
                        }
                        catch (Exception ex)
                        {
                            _data.Error = ex.Message;
                            Thread.Sleep(1000);
                        }
                    
                    }
                   
                }
                System.IO.File.WriteAllText($"{tmpFolder}\\index.txt" , m3u8filelist.ToString());
                _data.Title = "正在合并...";
                //-acodec copy -vcodec copy -absf aac_adtstoasc
                System.Diagnostics.Process.Start($"{AppDomain.CurrentDomain.BaseDirectory}ffmpeg.exe", $"-f concat -safe 0 -i \"{tmpFolder}\\index.txt\" -acodec copy -vcodec copy -absf aac_adtstoasc \"{_data.SavePath}\"").WaitForExit();
                try
                {
                    System.IO.Directory.Delete(tmpFolder, true);
                }
                catch
                {

                }
                _data.Title = "Finish";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                btnDownload.IsEnabled = true;
            }
        }

        private void btnBrowser(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.SaveFileDialog fd = new System.Windows.Forms.SaveFileDialog())
            {
                fd.Filter = "*.mp4|*.mp4";
                if(fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _data.SavePath = fd.FileName;
                }
            }
        }
    }

    class Model : Way.Lib.DataModel
    {
        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                base.OnPropertyChanged("Title", null, value);
            }
        }
        private string _error;
        public string Error
        {
            get => _error;
            set
            {
                if (_error != value)
                {
                    _error = value;
                    base.OnPropertyChanged("Error", null, value);
                }
            }
        }
        private string _Url = "";
        public string Url
        {
            get => _Url;
            set
            {
                _Url = value;
                base.OnPropertyChanged("Url", null, value);
            }
        }

        private string _savePath = "";
        public string SavePath
        {
            get => _savePath;
            set
            {
                _savePath = value;
                base.OnPropertyChanged("SavePath", null, value);
            }
        }
    }
}
