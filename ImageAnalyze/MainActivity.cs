using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Plugin.Media;
using Android;

namespace ImageAnalyze
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {

        readonly string[] permissionGroup =
        {
            Manifest.Permission.ReadExternalStorage,
            Manifest.Permission.WriteExternalStorage,
            Manifest.Permission.Camera
        };

        // La clé de Computer Vision : 

        const string subscriptionKey = "69af5f904d0144429767fc5b2b22b710";
        const string uriBase = "https://southcentralus.api.cognitive.microsoft.com/vision/v2.0/analyze";

        Bitmap mBitmap;
        private ImageView imageView;
        private ProgressBar progressBar;
        ByteArrayContent content;
        private TextView textView;
        // LES BOUTONS : 
        Button captureButton;
        Button uploadButton;
        Button btnAnalyze;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            
            mBitmap = BitmapFactory.DecodeResource(Resources, Resource.Drawable.Bill);
            captureButton = (Button)FindViewById(Resource.Id.captureButton);
            uploadButton = (Button)FindViewById(Resource.Id.uploadButton);
            imageView = FindViewById<ImageView>(Resource.Id.imgView);
            imageView.SetImageBitmap(mBitmap);
            textView = FindViewById<TextView>(Resource.Id.txtDescription);
            progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);
            btnAnalyze = FindViewById<Button>(Resource.Id.btnAnalyze);
            byte[] bitmapData;
            using (var stream = new MemoryStream())
            {
                mBitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                bitmapData = stream.ToArray();
            }
            content = new ByteArrayContent(bitmapData);

            captureButton.Click += CaptureButton_Click;
            uploadButton.Click += UploadButton_Click;
            RequestPermissions(permissionGroup, 0);

            btnAnalyze.Click += async delegate
            {
                busy();
               await MakeAnalysisRequest(content);
            };           
    }
        public async Task MakeAnalysisRequest(ByteArrayContent content)
        {
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", subscriptionKey);

                string requestParameters =
                    "visualFeatures=Description&details=Landmarks&language=en";

                // Assemble the URI for the REST API method.
                string uri = uriBase + "?" + requestParameters;

                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");
                
                // Asynchronously call the REST API method.
                var response = await client.PostAsync(uri, content);

                // Asynchronously get the JSON response.
                

                string contentString = await response.Content.ReadAsStringAsync();

                var analysesResult = JsonConvert.DeserializeObject<AnalysisModel>(contentString);
                NotBusy();
                textView.Text = analysesResult.description.captions[0].text.ToString();
            }
            catch (Exception e)
            {
                Toast.MakeText(this, "" + e.ToString(), ToastLength.Short).Show();
            }
        }
        private void UploadButton_Click(object sender, System.EventArgs e)
        {
            UploadPhoto();
        }

        private void CaptureButton_Click(object sender, System.EventArgs e)
        {
            TakePhoto();
        }
        async void TakePhoto()
        {
            await CrossMedia.Current.Initialize();

            var file = await CrossMedia.Current.TakePhotoAsync(new Plugin.Media.Abstractions.StoreCameraMediaOptions
            {
                PhotoSize = Plugin.Media.Abstractions.PhotoSize.Medium,
                CompressionQuality = 40,
                Name = "myimage.jpg",
                Directory = "sample"

            });

            if (file == null)
            {
                return;
            }

            // Convert file to byte array and set the resulting bitmap to imageview
            byte[] imageArray = System.IO.File.ReadAllBytes(file.Path);
            Bitmap bitmap = BitmapFactory.DecodeByteArray(imageArray, 0, imageArray.Length);
            imageView.SetImageBitmap(bitmap);
            content = new ByteArrayContent(imageArray);

        }

        async void UploadPhoto()
        {
            await CrossMedia.Current.Initialize();

            if (!CrossMedia.Current.IsPickPhotoSupported)
            {
                Toast.MakeText(this, "Upload not supported on this device", ToastLength.Short).Show();
                return;
            }

            var file = await CrossMedia.Current.PickPhotoAsync(new Plugin.Media.Abstractions.PickMediaOptions
            {
                PhotoSize = Plugin.Media.Abstractions.PhotoSize.Full,
                CompressionQuality = 40

            });

            // Convert file to byre array, to bitmap and set it to our ImageView

            byte[] imageArray = System.IO.File.ReadAllBytes(file.Path);
            Bitmap bitmap = BitmapFactory.DecodeByteArray(imageArray, 0, imageArray.Length);
            imageView.SetImageBitmap(bitmap);
            content = new ByteArrayContent(imageArray);
        }

        void busy()
        {
            progressBar.Visibility = ViewStates.Visible;
            btnAnalyze.Enabled = false;
        }

        void NotBusy()
        {
            progressBar.Visibility = ViewStates.Invisible;
            btnAnalyze.Enabled = true;
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}

