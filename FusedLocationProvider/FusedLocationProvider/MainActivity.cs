using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Gms.Common;
using Android.Gms.Location;
using Android.OS;
using Android.Util;
using Android.Widget;

namespace FusedLocationProvider
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private static readonly long FIVE_MINUTES = 5 * 60 * 1000;
        private static readonly long TWO_MINUTES = 2 * 60 * 1000;
        private static readonly string KEY_REQUESTING_LOCATION_UPDATES = "requesting_location_updates";

        private bool isGooglePlayServicesInstalled;

        private FusedLocationProviderClient fusedLocationProviderClient;
        private bool isRequestingLocationUpdates;
        private LocationCallback locationCallback;
        private LocationRequest locationRequest;
        private Task locationTask;

        private Button requestLocationUpdatesButton;
        internal TextView latitude2;
        internal TextView longitude2;
        internal TextView provider2;

        private Button getLastLocationButton;
        private TextView latitude;
        private TextView longitude;
        private TextView provider;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            if (bundle != null)
                isRequestingLocationUpdates = bundle.KeySet().Contains(KEY_REQUESTING_LOCATION_UPDATES) &&
                                              bundle.GetBoolean(KEY_REQUESTING_LOCATION_UPDATES);

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

            isGooglePlayServicesInstalled = IsGooglePlayServicesInstalled();

            if (isGooglePlayServicesInstalled)
            {
                locationRequest = new LocationRequest()
                    .SetPriority(LocationRequest.PriorityHighAccuracy)
                    .SetInterval(FIVE_MINUTES)
                    .SetFastestInterval(TWO_MINUTES);
                locationCallback = new LL(this);

                fusedLocationProviderClient = LocationServices.GetFusedLocationProviderClient(this);
                getLastLocationButton.Click += GetLastLocationButtonOnClick;
                requestLocationUpdatesButton.Click += RequestLocationUpdatesButtonOnClick;
            }
            else
            {
                Toast.MakeText(this, Resource.String.missing_gps_terminating, ToastLength.Long).Show();
                Finish();
            }
        }

        private void RequestLocationUpdatesButtonOnClick(object sender, EventArgs eventArgs)
        {
            // No need to request location updates if we're already doing so.
            if (isRequestingLocationUpdates) return;

            isRequestingLocationUpdates = true;
            locationTask = fusedLocationProviderClient.RequestLocationUpdatesAsync(locationRequest, locationCallback);
        }

        private async void GetLastLocationButtonOnClick(object sender, EventArgs eventArgs)
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

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutBoolean(KEY_REQUESTING_LOCATION_UPDATES, isRequestingLocationUpdates);
            base.OnSaveInstanceState(outState);
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (isRequestingLocationUpdates)
                locationTask =
                    fusedLocationProviderClient.RequestLocationUpdatesAsync(locationRequest, locationCallback);
        }

        #region Overrides of Activity

        protected override void OnPause()
        {
            isRequestingLocationUpdates = false;
            fusedLocationProviderClient.RemoveLocationUpdates(locationCallback);
            base.OnPause();
        }

        #endregion

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

    public class LL : LocationCallback
    {
        private readonly MainActivity activity;

        public LL(MainActivity activity)
        {
            this.activity = activity;
        }


        public override void OnLocationResult(LocationResult result)
        {
            if (result.Locations.Any())
            {
                var location = result.Locations.First();
                activity.latitude2.Text = activity.Resources.GetString(Resource.String.latitude_string, location.Latitude);
                activity.longitude2.Text = activity.Resources.GetString(Resource.String.longitude_string, location.Longitude);
                activity.provider2.Text = activity.Resources.GetString(Resource.String.requesting_updates_provider_string, location.Provider);
            }
            else
            {
                activity.latitude2.SetText(Resource.String.location_unavailable);
                activity.longitude2.SetText(Resource.String.location_unavailable);
                activity.provider2.SetText(Resource.String.could_not_get_last_location);
            }
        }
    }
}