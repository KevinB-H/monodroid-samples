using System;
using System.Threading.Tasks;

using Android;
using Android.App;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Gms.Location;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace FusedLocationProvider
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private const long ONE_MINUTE = 60 * 1000;
        private const long FIVE_MINUTES = 5 * ONE_MINUTE;
        private const long TWO_MINUTES = 2 * ONE_MINUTE;

        private static readonly int RC_PERMISSIONS_CHECK = 1000;
//        private static readonly int RC_PERMISSION_FOR_LAST_LOCATION = 1000;
//        private static readonly int RC_PERMISSION_FOR_LOCATION_UPDATES = 1100;

        private static readonly string KEY_REQUESTING_LOCATION_UPDATES = "requesting_location_updates";

        private FusedLocationProviderClient fusedLocationProviderClient;
        private Task locationTask;
        private Button getLastLocationButton;
        private bool isGooglePlayServicesInstalled;
        private bool isRequestingLocationUpdates;
        private TextView latitude;
        internal TextView latitude2;
        private LocationCallback locationCallback;
        private LocationRequest locationRequest;
        private TextView longitude;
        internal TextView longitude2;
        private TextView provider;
        internal TextView provider2;

        internal Button requestLocationUpdatesButton;

        private View rootLayout;


        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            if (bundle != null)
                isRequestingLocationUpdates = bundle.KeySet().Contains(KEY_REQUESTING_LOCATION_UPDATES) &&
                                              bundle.GetBoolean(KEY_REQUESTING_LOCATION_UPDATES);
            else
                isRequestingLocationUpdates = false;


            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            isGooglePlayServicesInstalled = IsGooglePlayServicesInstalled();
            rootLayout = FindViewById(Resource.Id.root_layout);

            // UI to display last location
            getLastLocationButton = FindViewById<Button>(Resource.Id.get_last_location_button);
            latitude = FindViewById<TextView>(Resource.Id.latitude);
            longitude = FindViewById<TextView>(Resource.Id.longitude);
            provider = FindViewById<TextView>(Resource.Id.provider);

            // UI to display location updates
            requestLocationUpdatesButton = FindViewById<Button>(Resource.Id.request_location_updates_button);
            latitude2 = FindViewById<TextView>(Resource.Id.latitude2);
            longitude2 = FindViewById<TextView>(Resource.Id.longitude2);
            provider2 = FindViewById<TextView>(Resource.Id.provider2);

            if (isGooglePlayServicesInstalled)
            {
                locationRequest = new LocationRequest()
                    .SetPriority(LocationRequest.PriorityHighAccuracy)
                    .SetInterval(FIVE_MINUTES)
                    .SetFastestInterval(TWO_MINUTES);
                locationCallback = new FusedLocationProviderCallback(this);

                fusedLocationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
                getLastLocationButton.Click += GetLastLocationButtonOnClick;
                requestLocationUpdatesButton.Click += RequestLocationUpdatesButtonOnClick;
            }
            else
            {
                Snackbar.Make(rootLayout, Resource.String.missing_googleplayservices_terminating, Snackbar.LengthIndefinite)
                    .SetAction(Resource.String.ok, delegate { Finish(); })
                    .Show();
            }
        }

        private async void RequestLocationUpdatesButtonOnClick(object sender, EventArgs eventArgs)
        {
            // No need to request location updates if we're already doing so.
            if (isRequestingLocationUpdates)
            {
                isRequestingLocationUpdates = false;
                fusedLocationProviderClient.RemoveLocationUpdates(locationCallback);
            }
            else
            {
                isRequestingLocationUpdates = true;
                await fusedLocationProviderClient.RequestLocationUpdatesAsync(locationRequest, locationCallback);
            }
        }

        private async void GetLastLocationButtonOnClick(object sender, EventArgs eventArgs)
        {
            await GetLastLocationFromDevice();
        }

        private async Task GetLastLocationFromDevice()
        {
            getLastLocationButton.SetText(Resource.String.getting_last_location);
            var location = await fusedLocationProviderClient.GetLastLocationAsync();

            if (location == null)
            {
                latitude.SetText(Resource.String.location_unavailable);
                longitude.SetText(Resource.String.location_unavailable);
                provider.SetText(Resource.String.could_not_get_last_location);
            }
            else
            {
                latitude.Text = Resources.GetString(Resource.String.latitude_string, location.Latitude);
                longitude.Text = Resources.GetString(Resource.String.longitude_string, location.Longitude);
                provider.Text = Resources.GetString(Resource.String.provider_string, location.Provider);
                getLastLocationButton.SetText(Resource.String.get_last_location_button_text);
            }
        }

        private void RequestLocationPermission()
        {
            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, Manifest.Permission.AccessFineLocation))
                Snackbar.Make(rootLayout, Resource.String.permission_location_rationale, Snackbar.LengthIndefinite)
                    .SetAction(Resource.String.ok,
                        delegate
                        {
                            ActivityCompat.RequestPermissions(this, new[] {Manifest.Permission.Camera}, RC_PERMISSIONS_CHECK);
                        })
                    .Show();
            else
                ActivityCompat.RequestPermissions(this, new[] {Manifest.Permission.Camera}, RC_PERMISSIONS_CHECK);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            if (requestCode == RC_PERMISSIONS_CHECK)
            {
                if (grantResults.Length == 1 && grantResults[0] != Permission.Granted)
                {
                    Finish();
                }
            }
            else
            {
                Log.Debug("FusedLocationProvider", "Don't know how to handle requestCode " + requestCode);
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void StartRequestingLocationUpdates()
        {
            requestLocationUpdatesButton.SetText(Resource.String.request_location_in_progress_button_text);
            locationTask = fusedLocationProviderClient.RequestLocationUpdatesAsync(locationRequest, locationCallback);
        }

        private void StopRequestionLocationUpdates()
        {
            requestLocationUpdatesButton.SetText(Resource.String.request_location_button_text);
            fusedLocationProviderClient.RemoveLocationUpdatesAsync(locationCallback);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutBoolean(KEY_REQUESTING_LOCATION_UPDATES, isRequestingLocationUpdates);
            base.OnSaveInstanceState(outState);
        }

        protected override void OnResume()
        {
            base.OnResume();

            var p = CheckSelfPermission(Manifest.Permission.AccessFineLocation);

            bool okay = Permission.Granted == p;
            if (okay)
            {
                if (isRequestingLocationUpdates)
                {
                    StartRequestingLocationUpdates();
                }
            }
            else
            {
                RequestLocationPermission();
            }
        }

        protected override void OnPause()
        {
            StopRequestionLocationUpdates();
            base.OnPause();
        }

        private bool IsGooglePlayServicesInstalled()
        {
            var queryResult = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (queryResult == ConnectionResult.Success)
            {
                Log.Info("MainActivity", "Google Play Services is installed on this device.");
                return true;
            }

            if (GoogleApiAvailability.Instance.IsUserResolvableError(queryResult))
            {
                var errorString = GoogleApiAvailability.Instance.GetErrorString(queryResult);
                Log.Error("ManActivity", "There is a problem with Google Play Services on this device: {0} - {1}",
                    queryResult, errorString);

                // Show error dialog to let user debug google play services
            }

            return false;
        }
    }
}