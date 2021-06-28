//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Web;

namespace SDKTemplate
{
    public sealed class StreamUriResolver : IUriToStreamResolver
    {
        public IAsyncOperation<IInputStream> UriToStreamAsync(Uri uri)
        {
            string path = uri.AbsolutePath;
            return GetContentAsync(path).AsAsyncOperation();
        }

        private async Task<IInputStream> GetContentAsync(string URIPath)
        {
            try
            {
                Uri localUri = new Uri("ms-appdata:///temp/folder/" + URIPath);
                StorageFile f = await StorageFile.GetFileFromApplicationUriAsync(localUri);
                IRandomAccessStream stream = await f.OpenAsync(FileAccessMode.Read);
                return stream.GetInputStreamAt(0);
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("Invalid URI");
            }
            return null;
        }
    }

    public sealed partial class ShareTarget : Page
    {
        ShareOperation shareOperation;
        private string sharedDataTitle;
        private string sharedDataDescription;
        private string sharedDataPackageFamilyName;
        private Uri sharedDataContentSourceWebLink;
        private Uri sharedDataContentSourceApplicationLink;
        private Color sharedDataLogoBackgroundColor;
        private IRandomAccessStreamReference sharedDataSquare30x30Logo;
        private string shareQuickLinkId;
        private string sharedText;
        private Uri sharedWebLink;
        private Uri sharedApplicationLink;
        private IReadOnlyList<IStorageItem> sharedStorageItems;
        private string sharedCustomData;
        private string sharedHtmlFormat;
        private IReadOnlyDictionary<string, RandomAccessStreamReference> sharedResourceMap;
        private IRandomAccessStreamReference sharedBitmapStreamRef;
        private IRandomAccessStreamReference sharedThumbnailStreamRef;
        private const string dataFormatName = "http://schema.org/Book";

        public ShareTarget()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // It is recommended to only retrieve the ShareOperation object in the activation handler, return as
            // quickly as possible, and retrieve all data from the share target asynchronously.

            this.shareOperation = (ShareOperation)e.Parameter;

            await Task.Factory.StartNew(async () =>
            {
                // Retrieve the data package properties.
                this.sharedDataTitle = this.shareOperation.Data.Properties.Title;
                this.sharedDataDescription = this.shareOperation.Data.Properties.Description;
                this.sharedDataPackageFamilyName = this.shareOperation.Data.Properties.PackageFamilyName;
                this.sharedDataContentSourceWebLink = this.shareOperation.Data.Properties.ContentSourceWebLink;
                this.sharedDataContentSourceApplicationLink = this.shareOperation.Data.Properties.ContentSourceApplicationLink;
                this.sharedDataLogoBackgroundColor = this.shareOperation.Data.Properties.LogoBackgroundColor;
                this.sharedDataSquare30x30Logo = this.shareOperation.Data.Properties.Square30x30Logo;
                this.sharedThumbnailStreamRef = this.shareOperation.Data.Properties.Thumbnail;
                this.shareQuickLinkId = this.shareOperation.QuickLinkId;

                // Retrieve the data package content.
                // The GetWebLinkAsync(), GetTextAsync(), GetStorageItemsAsync(), etc. APIs will throw if there was an error retrieving the data from the source app.
                // In this sample, we just display the error. It is recommended that a share target app handles these in a way appropriate for that particular app.
                if (this.shareOperation.Data.Contains(StandardDataFormats.WebLink))
                {
                    try
                    {
                        this.sharedWebLink = await this.shareOperation.Data.GetWebLinkAsync();
                    }
                    catch (Exception ex)
                    {
                        NotifyUserBackgroundThread("Failed GetWebLinkAsync - " + ex.Message, NotifyType.ErrorMessage);
                    }
                }
                if (this.shareOperation.Data.Contains(StandardDataFormats.ApplicationLink))
                {
                    try
                    {
                        this.sharedApplicationLink = await this.shareOperation.Data.GetApplicationLinkAsync();
                    }
                    catch (Exception ex)
                    {
                        NotifyUserBackgroundThread("Failed GetApplicationLinkAsync - " + ex.Message, NotifyType.ErrorMessage);
                    }
                }
                if (this.shareOperation.Data.Contains(StandardDataFormats.Text))
                {
                    try
                    {
                        this.sharedText = await this.shareOperation.Data.GetTextAsync();
                    }
                    catch (Exception ex)
                    {
                        NotifyUserBackgroundThread("Failed GetTextAsync - " + ex.Message, NotifyType.ErrorMessage);
                    }
                }
                if (this.shareOperation.Data.Contains(StandardDataFormats.StorageItems))
                {
                    try
                    {
                        this.sharedStorageItems = await this.shareOperation.Data.GetStorageItemsAsync();
                    }
                    catch (Exception ex)
                    {
                        NotifyUserBackgroundThread("Failed GetStorageItemsAsync - " + ex.Message, NotifyType.ErrorMessage);
                    }
                }
                if (this.shareOperation.Data.Contains(dataFormatName))
                {
                    try
                    {
                        this.sharedCustomData = await this.shareOperation.Data.GetTextAsync(dataFormatName);
                    }
                    catch (Exception ex)
                    {
                        NotifyUserBackgroundThread("Failed GetTextAsync(" + dataFormatName + ") - " + ex.Message, NotifyType.ErrorMessage);
                    }
                }
                if (this.shareOperation.Data.Contains(StandardDataFormats.Html))
                {
                    try
                    {
                        this.sharedHtmlFormat = await this.shareOperation.Data.GetHtmlFormatAsync();
                    }
                    catch (Exception ex)
                    {
                        NotifyUserBackgroundThread("Failed GetHtmlFormatAsync - " + ex.Message, NotifyType.ErrorMessage);
                    }

                    try
                    {
                        this.sharedResourceMap = await this.shareOperation.Data.GetResourceMapAsync();
                    }
                    catch (Exception ex)
                    {
                        NotifyUserBackgroundThread("Failed GetResourceMapAsync - " + ex.Message, NotifyType.ErrorMessage);
                    }
                }
                if (this.shareOperation.Data.Contains(StandardDataFormats.Bitmap))
                {
                    try
                    {
                        this.sharedBitmapStreamRef = await this.shareOperation.Data.GetBitmapAsync();
                    }
                    catch (Exception ex)
                    {
                        NotifyUserBackgroundThread("Failed GetBitmapAsync - " + ex.Message, NotifyType.ErrorMessage);
                    }
                }

                // In this sample, we just display the shared data content.

                // Get back to the UI thread using the dispatcher.
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    DataPackageTitle.Text = this.sharedDataTitle;
                    DataPackageDescription.Text = this.sharedDataDescription;
                    DataPackagePackageFamilyName.Text = this.sharedDataPackageFamilyName;
                    if (this.sharedDataContentSourceWebLink != null)
                    {
                        DataPackageContentSourceWebLink.Text = this.sharedDataContentSourceWebLink.AbsoluteUri;
                    }
                    if (this.sharedDataContentSourceApplicationLink != null)
                    {
                        DataPackageContentSourceApplicationLink.Text = this.sharedDataContentSourceApplicationLink.AbsoluteUri;
                    }

                    if (this.sharedDataSquare30x30Logo != null)
                    {
                        IRandomAccessStreamWithContentType logoStream = await this.sharedDataSquare30x30Logo.OpenReadAsync();
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.SetSource(logoStream);
                        LogoHolder.Source = bitmapImage;
                        LogoBackground.Background = new SolidColorBrush(this.sharedDataLogoBackgroundColor);
                        LogoArea.Visibility = Visibility.Visible;
                    }

                    if (!String.IsNullOrEmpty(this.shareOperation.QuickLinkId))
                    {
                        SelectedQuickLinkId.Text = this.shareQuickLinkId;
                    }

                    if (this.sharedThumbnailStreamRef != null)
                    {
                        IRandomAccessStreamWithContentType thumbnailStream = await this.sharedThumbnailStreamRef.OpenReadAsync();
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.SetSource(thumbnailStream);
                        ThumbnailHolder.Source = bitmapImage;
                        ThumbnailArea.Visibility = Visibility.Visible;
                    }

                    if (this.sharedWebLink != null)
                    {
                        AddContentValue("WebLink: ", this.sharedWebLink.AbsoluteUri);
                    }
                    if (this.sharedApplicationLink != null)
                    {
                        AddContentValue("ApplicationLink: ", this.sharedApplicationLink.AbsoluteUri);
                    }
                    if (this.sharedText != null)
                    {
                        AddContentValue("Text: ", this.sharedText);
                    }
                    if (this.sharedStorageItems != null)
                    {
                        // Display the name of the files being shared.
                        StringBuilder fileNames = new StringBuilder();
                        for (int index = 0; index < this.sharedStorageItems.Count; index++)
                        {
                            fileNames.Append(this.sharedStorageItems[index].Name);
                            if (index < this.sharedStorageItems.Count - 1)
                            {
                                fileNames.Append(", ");
                            }
                        }
                        fileNames.Append(".");

                        AddContentValue("StorageItems: ", fileNames.ToString());
                    }
                    if (this.sharedCustomData != null)
                    {
                        // This is an area to be especially careful parsing data from the source app to avoid buffer overruns.
                        // This sample doesn't perform data validation but will catch any exceptions thrown.
                        try
                        {
                            StringBuilder receivedStrings = new StringBuilder();
                            JsonObject customObject = JsonObject.Parse(this.sharedCustomData);
                            if (customObject.ContainsKey("type"))
                            {
                                if (customObject["type"].GetString() == "http://schema.org/Book")
                                {
                                    // This sample expects the custom format to be of type http://schema.org/Book
                                    receivedStrings.AppendLine("Type: " + customObject["type"].Stringify());
                                    JsonObject properties = customObject["properties"].GetObject();
                                    receivedStrings.AppendLine("Image: " + properties["image"].Stringify());
                                    receivedStrings.AppendLine("Name: " + properties["name"].Stringify());
                                    receivedStrings.AppendLine("Book Format: " + properties["bookFormat"].Stringify());
                                    receivedStrings.AppendLine("Author: " + properties["author"].Stringify());
                                    receivedStrings.AppendLine("Number of Pages: " + properties["numberOfPages"].Stringify());
                                    receivedStrings.AppendLine("Publisher: " + properties["publisher"].Stringify());
                                    receivedStrings.AppendLine("Date Published: " + properties["datePublished"].Stringify());
                                    receivedStrings.AppendLine("In Language: " + properties["inLanguage"].Stringify());
                                    receivedStrings.Append("ISBN: " + properties["isbn"].Stringify());

                                    AddContentValue("Custom format data:" + Environment.NewLine, receivedStrings.ToString());
                                }
                                else
                                {
                                    NotifyUser("The custom format from the source app is not of type http://schema.org/Book", NotifyType.ErrorMessage);
                                }
                            }
                            else
                            {
                                NotifyUser("The custom format from the source app doesn't contain a type", NotifyType.ErrorMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            NotifyUser("Failed to parse the custom data - " + ex.Message, NotifyType.ErrorMessage);
                        }
                    }
                    if (this.sharedHtmlFormat != null)
                    {
                        string htmlFragment = HtmlFormatHelper.GetStaticFragment(this.sharedHtmlFormat);
                        if (!String.IsNullOrEmpty(htmlFragment))
                        {
                            AddContentValue("HTML: ");
                            ShareWebView.Visibility = Visibility.Visible;
                            // Check if there are any local images in the resource map.
                            if (this.sharedResourceMap.Count > 0)
                            {
                                foreach (KeyValuePair<string, RandomAccessStreamReference> item in this.sharedResourceMap)
                                {
                                    var stream = await item.Value.OpenReadAsync();
                                    String strContentType = stream.ContentType;

                                    if (!strContentType.StartsWith("image/"))
                                        continue;

                                    int indexOfComma = strContentType.IndexOf(",");
                                    String strImageType;

                                    if (indexOfComma == -1)
                                        strImageType = strContentType;
                                    else
                                        strImageType = strContentType.Substring(0, indexOfComma);

                                    var reader = new DataReader(stream.GetInputStreamAt(0));
                                    await reader.LoadAsync((uint)stream.Size);
                                    byte[] byteArray = new byte[stream.Size];
                                    reader.ReadBytes(byteArray);
                                    String base64String = "'data:" + strImageType + ";base64," + Convert.ToBase64String(byteArray) + "'";

                                    htmlFragment = htmlFragment.Replace(item.Key, base64String);
                                }
                            }

                            var folder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync("folder", CreationCollisionOption.OpenIfExists);
                            var file = await folder.CreateFileAsync("index.html", CreationCollisionOption.OpenIfExists);
                            await FileIO.WriteTextAsync(file, htmlFragment);

                            var uri = ShareWebView.BuildLocalStreamUri("InternalAssets", "index.html");
                            ShareWebView.NavigateToLocalStreamUri(uri, new StreamUriResolver());
                        }
                        else
                        {
                            NotifyUser("GetStaticFragment failed to parse the HTML from the source app", NotifyType.ErrorMessage);
                        }

                        // Check if there are any local images in the resource map.
                        if (this.sharedResourceMap.Count > 0)
                        {
                            ResourceMapValue.Text = "";
                            foreach (KeyValuePair<string, RandomAccessStreamReference> item in this.sharedResourceMap)
                            {
                                ResourceMapValue.Text += "\nKey: " + item.Key;
                            }
                            ResourceMapArea.Visibility = Visibility.Visible;
                        }
                    }
                    if (this.sharedBitmapStreamRef != null)
                    {
                        IRandomAccessStreamWithContentType bitmapStream = await this.sharedBitmapStreamRef.OpenReadAsync();
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.SetSource(bitmapStream);
                        ImageHolder.Source = bitmapImage;
                        ImageArea.Visibility = Visibility.Visible;
                    }
                });
            });
        }

        private void QuickLinkSectionLabel_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Trigger the appropriate Checked/Unchecked event if the user taps on the text instead of the checkbox.
            AddQuickLink.IsChecked = !AddQuickLink.IsChecked;
        }

        private void AddQuickLink_Checked(object sender, RoutedEventArgs e)
        {
            QuickLinkCustomization.Visibility = Visibility.Visible;
        }

        private void AddQuickLink_Unchecked(object sender, RoutedEventArgs e)
        {
            QuickLinkCustomization.Visibility = Visibility.Collapsed;
        }

        async void ReportCompleted_Click(object sender, RoutedEventArgs e)
        {
            if (AddQuickLink.IsChecked.Equals(true))
            {
                QuickLink quickLinkInfo = new QuickLink
                {
                    Id = QuickLinkId.Text,
                    Title = QuickLinkTitle.Text,

                    // For QuickLinks, the supported FileTypes and DataFormats are set independently from the manifest.
                    SupportedFileTypes = { "*" },
                    SupportedDataFormats =
                    {
                        StandardDataFormats.Text,
                        StandardDataFormats.WebLink,
                        StandardDataFormats.ApplicationLink,
                        StandardDataFormats.Bitmap,
                        StandardDataFormats.StorageItems,
                        StandardDataFormats.Html,
                        dataFormatName
                    }
                };

                try
                {
                    StorageFile iconFile = await Package.Current.InstalledLocation.CreateFileAsync("assets\\squareTile-sdk.png", CreationCollisionOption.OpenIfExists);
                    quickLinkInfo.Thumbnail = RandomAccessStreamReference.CreateFromFile(iconFile);
                    this.shareOperation.ReportCompleted(quickLinkInfo);
                }
                catch (Exception)
                {
                    // Even if the QuickLink cannot be created it is important to call ReportCompleted. Otherwise, if this is a long-running share,
                    // the app will stick around in the long-running share progress list.
                    this.shareOperation.ReportCompleted();
                    throw;
                }
            }
            else
            {
                this.shareOperation.ReportCompleted();
            }
        }

        private void DismissUI_Click(object sender, RoutedEventArgs e)
        {
            this.shareOperation.DismissUI();
        }

        private void OtherShareOperations_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Trigger the appropriate Checked/Unchecked event if the user taps on the text instead of the checkbox.
            OtherShareOperationMethodsSection.IsChecked = !OtherShareOperationMethodsSection.IsChecked;
        }

        private void OtherShareOperationMethodsSection_Unchecked(object sender, RoutedEventArgs e)
        {
            ExtendedSharingArea.Visibility = Visibility.Collapsed;
        }

        private void OtherShareOperationMethodsSection_Checked(object sender, RoutedEventArgs e)
        {
            ExtendedSharingArea.Visibility = Visibility.Visible;
        }

        private void ReportDataRetrieved_Click(object sender, RoutedEventArgs e)
        {
            this.shareOperation.ReportDataRetrieved();
            this.NotifyUser("Data retrieved...", NotifyType.StatusMessage);
        }

        private void ReportErrorButton_Click(object sender, RoutedEventArgs e)
        {
            this.shareOperation.ReportError(ReportError.Text);
        }

        async private void Footer_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(((HyperlinkButton)sender).Tag.ToString()));
        }

        private void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                // Use the status message style.
                case NotifyType.StatusMessage:
                    StatusBlock.Style = Resources["StatusStyle"] as Style;
                    break;
                // Use the error message style.
                case NotifyType.ErrorMessage:
                    StatusBlock.Style = Resources["ErrorStyle"] as Style;
                    break;
            }
            StatusBlock.Text = strMessage;
        }

        async private void NotifyUserBackgroundThread(string message, NotifyType type)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                NotifyUser(message, type);
            });
        }

        private void AddContentValue(string title, string description = null)
        {
            Run contentType = new Run();
            contentType.FontWeight = FontWeights.Bold;
            contentType.Text = title;
            ContentValue.Inlines.Add(contentType);

            if (description != null)
            {
                Run contentValue = new Run();
                contentValue.Text = description + Environment.NewLine;
                ContentValue.Inlines.Add(contentValue);
            }
        }
    }
}
