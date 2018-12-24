using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;


namespace LifeOrganiser
{
    public partial class WebBrowser : Form
    {
        public ChromiumWebBrowser chromeBrowser;

        public RecipesPanel recipesPanel = new RecipesPanel();

        private MyDownloadHandler downloadHandler = new MyDownloadHandler();

        public void InitializeChromium()
        {
            CefSettings settings = new CefSettings();
            // Initialize cef with the provided settings
            Cef.Initialize(settings);
            // Create a browser component
            this.chromeBrowser = new ChromiumWebBrowser("https://www.google.ca/");
            // Add it to the form and fill it to the form window.
            this.toolStripContainer1.ContentPanel.Controls.Add(chromeBrowser);
            this.chromeBrowser.Dock = DockStyle.Fill;
            this.chromeBrowser.LoadingStateChanged += new EventHandler<LoadingStateChangedEventArgs>(this.chromeBrowser_LoadingStateChanged);
            this.chromeBrowser.AddressChanged += new EventHandler<AddressChangedEventArgs>(this.chromeBrowser_AddressChanged);
            
            //downloadHandler.OnBeforeDownloadFired += (o, downloadItem) =>
            //{
            //    Console.WriteLine($"{nameof(MyDownloadHandler.OnBeforeDownloadFired)}: {downloadItem.SuggestedFileName}");
            //};

            //downloadHandler.OnDownloadUpdatedFired += (o, downloadItem) =>
            //{
            //    Console.WriteLine($"{nameof(MyDownloadHandler.OnBeforeDownloadFired)}: {downloadItem.PercentComplete}");
            //};
        }

        public WebBrowser()
        {
            InitializeComponent();
            // Start the browser after initialize global component
            InitializeChromium();
            recipesPanel.FormClosing += this.RecipesPanel_FormClosing;
        }

        private void WebBrowser_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.recipesPanel.Close();
            Cef.Shutdown();
        }

        private void toolStripButtonBack_Click(object sender, EventArgs e)
        {
            this.chromeBrowser.Back();
        }

        private void toolStripButtonForward_Click(object sender, EventArgs e)
        {
            this.chromeBrowser.Forward();
        }

        private void toolStripButtonHome_Click(object sender, EventArgs e)
        {
            this.chromeBrowser.Load("https://www.ricardocuisine.com/");
        }

        private void toolStripButtonRefresh_Click(object sender, EventArgs e)
        {
            this.chromeBrowser.Refresh();
        }

        private void toolStripButtonStop_Click(object sender, EventArgs e)
        {
            this.chromeBrowser.Stop();
        }

        private void toolStripButtonNavigate_Click(object sender, EventArgs e)
        {
            this.Navigate(this.toolStripTextBoxUrl.Text);
        }

        private void chromeBrowser_AddressChanged(object sender, AddressChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => chromeBrowser_AddressChanged(sender, e)));
            }

            this.toolStripTextBoxUrl.Text = e.Address;
        }

        private void chromeBrowser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            //if (InvokeRequired)
            //{
            //    Invoke(new Action(() =>
            //    {
            //        if (e.IsLoading)
            //        {
            //            this.toolStripProgressBar.PerformStep();
            //        }
            //        else
            //        {
            //            this.toolStripProgressBar.Value = this.toolStripProgressBar.Maximum;
            //            ThreadPool.QueueUserWorkItem(new WaitCallback((state) =>
            //            {
            //                Thread.Sleep(300);
            //                if (InvokeRequired)
            //                {
            //                    Invoke(new Action(() =>
            //                    {
            //                        this.toolStripProgressBar.Value = this.toolStripProgressBar.Minimum;
            //                    }));
            //                }
            //            }));
            //        }
            //    }));
            //}
        }

        private void toolStripTextBoxUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.Navigate(this.toolStripTextBoxUrl.Text);
            }
        }

        private void Navigate(string address)
        {
            if (string.IsNullOrWhiteSpace(address) ||
                address.Equals("about:blank"))
            {
                return;
            }

            string url = address;

            //if (!address.StartsWith("www."))
            //{
            //    url = "www." + address;
            //}

            //if (!address.StartsWith("http://") &&
            //    !address.StartsWith("https://"))
            //{
            //    url = "http://" + url;
            //}
            
            try
            {
                this.chromeBrowser.Load(new Uri(url).AbsoluteUri);
            }
            catch (UriFormatException)
            {
                return;
            }
        }

        private void toolStripTextBoxSearchBar_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.toolStripTextBoxSearchBar.Text))
            {
                this.chromeBrowser.StopFinding(true);
            }
            else
            {
                this.chromeBrowser.Find(0, this.toolStripTextBoxSearchBar.Text, true, false, false);
            }
        }

        private void toolStripButtonSelect_Click(object sender, EventArgs e)
        {
            this.recipesPanel.GetWebContent(this.toolStripTextBoxUrl.Text);
        }

        private void toolStripButtonRecipes_Click(object sender, EventArgs e)
        {
            var button = sender as ToolStripButton;

            if (button.Text.Contains("Close"))
            {
                this.toolStripButtonRecipes.Text = button.Text.Replace("Close", "Open");
                this.recipesPanel.Hide();
            }
            else
            {
                this.toolStripButtonRecipes.Text = button.Text.Replace("Open", "Close");
                this.recipesPanel.Show();
            }
        }

        private void RecipesPanel_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.toolStripButtonRecipes.Text = this.toolStripButtonRecipes.Text.Replace("Close", "Open");
                this.recipesPanel.Hide();
            }
        }

        public class MyDownloadHandler : IDownloadHandler
        {
            public event EventHandler<DownloadItem> OnBeforeDownloadFired;

            public event EventHandler<DownloadItem> OnDownloadUpdatedFired;

            public void OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
            {
                OnBeforeDownloadFired?.Invoke(this, downloadItem);

                if (!callback.IsDisposed)
                {
                    using (callback)
                    {
                        callback.Continue(downloadItem.SuggestedFileName, showDialog: true);
                    }
                }
            }

            public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
            {
                OnDownloadUpdatedFired?.Invoke(this, downloadItem);
            }
        }
    }
}

